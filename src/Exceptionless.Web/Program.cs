using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.AspNetCore;
using App.Metrics.Formatters;
using App.Metrics.Formatters.Prometheus;
using Elastic.CommonSchema.Serilog;
using Elasticsearch.Net;
using Exceptionless.Core;
using Exceptionless.Core.Configuration;
using Exceptionless.Core.Extensions;
using Exceptionless.Insulation.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.Exceptionless;

namespace Exceptionless.Web {
    public class Program {
        public static async Task<int> Main(string[] args) {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try {
                await CreateHostBuilder(args).Build().RunAsync();
                return 0;
            } catch (Exception ex) {
                Log.Fatal(ex, "Job host terminated unexpectedly");
                return 1;
            } finally {
                Log.CloseAndFlush();
                await ExceptionlessClient.Default.ProcessQueueAsync();

                if (Debugger.IsAttached)
                    Console.ReadKey();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) {
            string environment = Environment.GetEnvironmentVariable("EX_AppMode");
            if (String.IsNullOrWhiteSpace(environment))
                environment = "Production";

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddYamlFile("appsettings.yml", optional: true, reloadOnChange: true)
                .AddYamlFile($"appsettings.{environment}.yml", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("EX_")
                .AddEnvironmentVariables("ASPNETCORE_")
                .AddCommandLine(args)
                .Build();

            return CreateHostBuilder(config, environment);
        }

        public static IHostBuilder CreateHostBuilder(IConfiguration config, string environment) {
            Console.Title = "Exceptionless Web";

            var options = AppOptions.ReadFromConfiguration(config);

            var configDictionary = config.ToDictionary("Serilog");
            Log.Information("Bootstrapping Exceptionless Web in {AppMode} mode ({InformationalVersion}) on {MachineName} with settings {@Settings}", environment, options.InformationalVersion, Environment.MachineName, configDictionary);

            var builder = Host.CreateDefaultBuilder()
                .UseEnvironment(environment)
                .ConfigureLogging(l => l.Configure(o => o.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId))
                .UseSerilog((context, services, configuration) => {
                    if (!String.IsNullOrEmpty(options.ExceptionlessApiKey))
                        configuration.WriteTo.Sink(new ExceptionlessSink(), LogEventLevel.Information);

                    var httpAccessor = context.Configuration.Get<HttpContextAccessor>();

                    // Create a formatter configuration to se this accessor
                    var formatterConfig = new EcsTextFormatterConfiguration();
                    formatterConfig.MapHttpContext(httpAccessor);

                    // Write events to the console using this configration
                    var formatter = new EcsTextFormatter(formatterConfig);

                    var elasticsearchConnectionString = config.GetConnectionString("Elasticsearch") ?? "http://localhost:9200";
                    configuration.WriteTo.Elasticsearch(
                        new ElasticsearchSinkOptions(new Uri(elasticsearchConnectionString)) {
                            CustomFormatter = formatter,
                            IndexFormat = $"{options.ElasticsearchOptions.ScopePrefix}-logs-web",
                            BatchAction = ElasticOpType.Create,
                            AutoRegisterTemplate = true,
                            OverwriteTemplate = true,
                            DetectElasticsearchVersion = true,
                            NumberOfReplicas = options.AppMode == AppMode.Development ? 0 : 1,
                            MinimumLogEventLevel = LogEventLevel.Verbose
                        });

                    configuration
                        .WriteTo.Console()
                        .ReadFrom.Configuration(config)
                        .ReadFrom.Services(services);
                })
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder
                        .UseConfiguration(config)
                        .ConfigureKestrel(c => {
                            c.AddServerHeader = false;

                            if (options.MaximumEventPostSize > 0)
                                c.Limits.MaxRequestBodySize = options.MaximumEventPostSize;
                        })
                        .UseStartup<Startup>();
                })
                .ConfigureServices((ctx, services) => {
                    services.AddSingleton(config);
                    services.AddAppOptions(options);
                    services.AddHttpContextAccessor();
                });

            if (!String.IsNullOrEmpty(options.MetricOptions.Provider))
                ConfigureMetricsReporting(builder, options.MetricOptions);

            return builder;
        }

        private static void ConfigureMetricsReporting(IHostBuilder builder, MetricOptions options) {
            if (String.Equals(options.Provider, "prometheus")) {
                var metrics = AppMetrics.CreateDefaultBuilder()
                    .OutputMetrics.AsPrometheusPlainText()
                    .OutputMetrics.AsPrometheusProtobuf()
                    .Build();
                builder.ConfigureMetrics(metrics).UseMetrics(o => {
                    o.EndpointOptions = endpointsOptions => {
                        endpointsOptions.MetricsTextEndpointOutputFormatter = metrics.OutputMetricsFormatters.GetType<MetricsPrometheusTextOutputFormatter>();
                        endpointsOptions.MetricsEndpointOutputFormatter = metrics.OutputMetricsFormatters.GetType<MetricsPrometheusProtobufOutputFormatter>();
                    };
                });
            } else if (!String.Equals(options.Provider, "statsd")) {
                builder.UseMetrics();
            }
        }
    }
}
