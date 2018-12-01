using Autofac;
using Commons.Logging;
using Commons.Logging.Autofac;
using $safeprojectname$.Configuration;

namespace $safeprojectname$
{
    internal static class CompositionRoot
    {
        public static void RegisterApplicationComponents(this ContainerBuilder builder)
        {
            builder.RegisterLogger(LoggerFactory.Instance);
            
            builder.RegisterConfigurations();
        }
    }
}