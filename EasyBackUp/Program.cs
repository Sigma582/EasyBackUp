using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace EasyBackUp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(new string[] { })
                .UseWindowsService(options =>
                {
                    options.ServiceName = "test";
                }
                )
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddHostedService<BackupService>();
                }
                )
                .Build();

            await host.RunAsync();
        }
    }
}
