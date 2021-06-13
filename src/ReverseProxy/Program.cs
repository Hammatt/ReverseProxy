using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReverseProxy.DomainLogic;
using ReverseProxy.Parser;

namespace ReverseProxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<HttpRequestParser>();
                    services.AddSingleton<HttpResponseParser>();

                    services.AddSingleton<DestinationService>();
                    services.AddSingleton<ReverseProxyService>();

                    services.AddSingleton<Sockets>();
                    services.AddHostedService<Worker>();
                });
    }
}
