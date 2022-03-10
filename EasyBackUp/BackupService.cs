using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace EasyBackUp
{
    public class BackupService : BackgroundService
    {
        private Worker _worker;

        public BackupService(ILogger<BackgroundService> logger)
        {
            Logger = logger;
            Logger.LogInformation("BackupService starting");
        }

        public ILogger<BackgroundService> Logger { get; }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _worker = new Worker(Logger);
            await _worker.ExecuteAsync(cancellationToken);
        }
    }
}
