using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Foundatio.Jobs;
using Foundatio.Queues;

namespace Exceptionless.Core.Jobs {
    public abstract class AppQueueJobBase<T> : QueueJobBase<T> where T: class {
        public AppQueueJobBase(IQueue<T> queue, ILoggerFactory loggerFactory = null) : base(queue, loggerFactory) { }

        public override Task<JobResult> RunAsync(CancellationToken cancellationToken = default) {
            using var activity = ActivitySources.JobActivitySource.StartActivity(GetType().Name);

            return base.RunAsync(cancellationToken);
        }
    }
}