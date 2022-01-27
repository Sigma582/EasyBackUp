using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EasyBackUp
{
    public class Worker
    {
        public async Task ExecuteAsync(CancellationToken? cancellationToken = null)
        {
            while (cancellationToken?.IsCancellationRequested != true)
            {
                var logPath = @"C:\Users\me\source\repos\EasyBackUp\EasyBackUp\bin\Debug\test.log";
                File.AppendAllText(logPath, $"{DateTime.Now:s} Running\r\n");
                await Task.Delay(1 * 1000);
            }
        }
    }
}
