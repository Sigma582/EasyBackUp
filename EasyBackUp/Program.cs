using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace EasyBackUp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u}] <THREAD: {ThreadId}> {Message:lj} {NewLine}{Exception}{NewLine}";

            var host = Host.CreateDefaultBuilder(new string[] { })
                .UseSerilog((hostingContext, loggerConfiguration) => 
                    loggerConfiguration
                        //.ReadFrom.Configuration(hostingContext.Configuration)
                        .MinimumLevel.Information()
                        .Enrich.FromLogContext()
                        .Enrich.WithThreadId()
                        .WriteTo.Console(outputTemplate: outputTemplate)
                        .WriteTo.File("log\\.log"
                            , outputTemplate: outputTemplate
                            , rollingInterval: RollingInterval.Day)
                        )
                .UseWindowsService(options =>
                {
                    options.ServiceName = "EasyBackUp";
                }
                )
                .ConfigureServices((context, services) =>
                {
                    services.Configure<List<TargetDefinition>>((targetDefinitions) =>
                    {
                        IConfiguration config = context.Configuration;
                        var section = config.GetSection("TargetDefinitions");
                        foreach (var definitionConfig in section.GetChildren())
                        {
                            var definition = new TargetDefinition();
                            definitionConfig.Bind(definition);
                            targetDefinitions.Add(definition);
                        }
                    });

                    services.AddHostedService<BackupService>();
                }
                )
                .Build();

            await host.RunAsync();
        }
    }
}
