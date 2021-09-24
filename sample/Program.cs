using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SilkierQuartz;
using SilkierQuartz.HostedService;

namespace SilkierQuartz.Example
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    if (args.Length > 1 && args[0] == "-p")
                    {
                        string portStr = args[1];
                        if (Regex.IsMatch(portStr, @"^\d*$"))
                        {
                            webBuilder.UseUrls($"http://*:{portStr}");
                        }
                    }
                    else
                    {
                        webBuilder.UseUrls($"http://*:5000");
                    }
                })
                .ConfigureSilkierQuartzHost();



    }
}
