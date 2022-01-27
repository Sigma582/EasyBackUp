using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace EasyBackUp
{
    public class BackupService : BackgroundService
    {
        private Worker _worker;

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _worker = new Worker();
            await _worker.ExecuteAsync(cancellationToken);
        }
    }
}
