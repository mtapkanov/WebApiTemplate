using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using App.Metrics;
using App.Metrics.AspNetCore;
using App.Metrics.Formatters;
using App.Metrics.Formatters.Prometheus;
using Autofac.Extensions.DependencyInjection;
using Commons.DataAccess;
using Commons.Logging;
using Commons.Logging.Serilog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Serilog;
using $safeprojectname$.Configuration;

namespace $safeprojectname$
{
    public class Program
    {
        public static IMetricsRoot Metrics { get; private set; }

        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                          .SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location))
                          .AddJsonFile("appsettings.json");

            if (args.Contains("--production"))
            {
                builder = builder.AddJsonFile("appsettings.production.json", optional: true);
            }

            ConfigurationFacade.Configuration = builder.Build();

            ConnectionStringsManager.ReadFromConfiguration(ConfigurationFacade.Configuration);

            LoggerFactory.Instance = CreateLoggerFactory(ConfigurationFacade.Configuration);

            Metrics = AppMetrics.CreateDefaultBuilder()
                                .OutputMetrics.AsPrometheusPlainText()
                                .Build();

            var host = new WebHostBuilder()
                       .UseKestrel()
                       .UseConfiguration(ConfigurationFacade.Configuration)
                       .ConfigureServices(s => s.AddAutofac())
                       .UseMetrics(options =>
                       {
                           options.EndpointOptions = endpointsOptions =>
                           {
                               endpointsOptions.MetricsTextEndpointOutputFormatter = Metrics.OutputMetricsFormatters.GetType<MetricsPrometheusTextOutputFormatter>();
                               endpointsOptions.MetricsEndpointOutputFormatter     = Metrics.OutputMetricsFormatters.GetType<MetricsPrometheusTextOutputFormatter>();
                           };
                       })
                       .UseStartup<Startup>()
                       .UseSerilog()
                       .UseUrls(ConfigurationFacade.ListenUri)
                       .Build();

            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            if (isService)
            {
                host.RunAsService();
            }
            else
            {
                Console.Title = "Service name";

                host.Run();
            }
        }

        private static ILoggerFactory CreateLoggerFactory(IConfiguration configuration)
        {
            var loggerConfiguration = new LoggerConfiguration()
                                      .Enrich.FromLogContext()
                                      .Enrich.WithAssemblyName()
                                      .Enrich.WithAssemblyVersion()
                                      .ReadFrom.Configuration(configuration);

            return new SerilogLoggerFactory(loggerConfiguration);
        }
    }
}