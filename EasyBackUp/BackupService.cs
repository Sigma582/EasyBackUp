using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyBackUp
{
    public class BackupService : BackgroundService
    {
        private Worker _worker;

        public BackupService(ILogger<BackgroundService> logger, IOptions<List<TargetDefinition>> options)
        {
            Logger = logger;
            Options = options;
            Logger.LogInformation("BackupService starting");
        }

        public ILogger<BackgroundService> Logger { get; }
        public IOptions<List<TargetDefinition>> Options { get; }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _worker = new Worker(Logger, Options);
            await _worker.ExecuteAsync(cancellationToken);
        }
    }
}
