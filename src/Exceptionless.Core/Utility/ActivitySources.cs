using System.Diagnostics;

namespace Exceptionless.Core {
    public static class ActivitySources {
        public static ActivitySource JobActivitySource = new ActivitySource("Exceptionless.Jobs");
        public static ActivitySource StartupActivitySource = new ActivitySource("Exceptionless.Startup");
        public static ActivitySource HealthCheckActivitySource = new ActivitySource("Exceptionless.HealthCheck");
    }
}
