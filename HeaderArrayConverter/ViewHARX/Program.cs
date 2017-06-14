using System.IO;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;

namespace ViewHARX
{
    [PublicAPI]
    public class Program
    {
        public static void Main(string[] args)
        {
            IWebHost host = 
                new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            host.Run();
        }
    }
}