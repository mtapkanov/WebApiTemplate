using Autofac;
using Microsoft.Extensions.Configuration;

namespace $safeprojectname$.Configuration
{
    public static class ConfigurationFacade
    {
        public static IConfiguration Configuration { get; internal set; }

        public static string ListenUri => Configuration.GetValue<string>("listen-uri");

        public static void RegisterConfigurations(this ContainerBuilder builder)
        {
            builder.RegisterInstance(Configuration);
        }

        private static void RegisterConfiguration<T>(this ContainerBuilder builder,
                                                     string section) where T : class
        {
            var configuration = Configuration.GetSection(section).Get<T>();

            builder.RegisterInstance(configuration);
        }
    }
}