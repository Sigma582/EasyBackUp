using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;

namespace EasyBackUp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //var configuration = new ConfigurationBuilder()
            //    .SetBasePath(Directory.GetCurrentDirectory())
            //    .AddJsonFile(path: "appsettings.json", reloadOnChange: true)
            //    .Build();

            var host = Host.CreateDefaultBuilder(new string[] { })
                .UseSerilog((hostingContext, loggerConfiguration) => 
                    loggerConfiguration
                        //.ReadFrom.Configuration(hostingContext.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console()
                        .WriteTo.File("log\\.log"
                            , outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u}] {Message:lj}{NewLine}{Exception}{NewLine}"
                            , rollingInterval: RollingInterval.Day)
                        )
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

        private static void Logging(ILoggingBuilder logging)
        {
        }
    }
}
