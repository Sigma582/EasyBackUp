using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyBackUp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (o, args) => { };
            
            var parameters = new Parameters(args);
            
            if (Environment.UserInteractive)
            {
                await RunConsole(parameters);
            }
            else
            {
                await RunService(parameters);
            }
        }

        private static async Task RunConsole(Parameters parameters)
        {
            Console.WriteLine("Running console");

            var service = new BackupService();
            var cancellationToken = new CancellationToken();
            service.StartAsync(cancellationToken);

            Console.WriteLine("Service started");
            Console.WriteLine("Press Esc to stop");

            ConsoleKeyInfo key = default;
            while (key.Key != ConsoleKey.Escape)
            {
                key = Console.ReadKey(true);
            }

            await service.StopAsync(new CancellationToken());
            Console.WriteLine("Stopped");
            Console.ReadLine();
        }

        private static async Task RunService(Parameters parameters)
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
