using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Testproject")]

namespace Beste.GameServer.SDaysTDie
{
    class Program
    {
        private static readonly char SEP = Path.DirectorySeparatorChar;
        static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options => options.ConfigureEndpoints())
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
