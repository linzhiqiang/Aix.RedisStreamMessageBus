using CommandLine;
using Aix.RedisStreamMessageBus.Sample.Model;
using Aix.RedisStreamMessageBus.Utils;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aix.RedisStreamMessageBus.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser parser = new Parser((setting) =>
            {
                setting.CaseSensitive = false;
            });

            parser.ParseArguments<CmdOptions>(args).WithParsed((options) =>
            {
                CmdOptions.Options = options;
                CreateHostBuilder(args).Build().Run();
            });

           
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureHostConfiguration(configurationBuilder =>
            {
               
            })
           .ConfigureAppConfiguration((hostBulderContext, configurationBuilder) =>
           {
              
           })
            .ConfigureLogging((hostBulderContext, loggingBuilder) =>
            {
               
            })
          
            .ConfigureServices(Startup.ConfigureServices);
    }
}
