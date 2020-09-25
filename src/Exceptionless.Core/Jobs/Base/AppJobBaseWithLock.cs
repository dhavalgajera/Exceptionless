using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Foundatio.Jobs;

namespace Exceptionless.Core.Jobs {
    public abstract class AppJobWithLockBase : JobWithLockBase {
        public AppJobWithLockBase(ILoggerFactory loggerFactory = null) : base(loggerFactory) { }

        public override Task<JobResult> RunAsync(CancellationToken cancellationToken = default) {
            using var activity = ActivitySources.JobActivitySource.StartActivity(GetType().Name);

            return base.RunAsync(cancellationToken);
        }
    }
}