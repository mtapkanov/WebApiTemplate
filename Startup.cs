using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Commons.DataAccess.AspNetCore;
using Commons.DataAccess.Contexts;
using Commons.DataAccess.Dapper;
using Commons.DataAccess.Oracle;
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using $safeprojectname$.Extensions;
using UtcDateTimeHandler = Commons.DataAccess.Dapper.UtcDateTimeHandler;

namespace $safeprojectname$
{
    public class Startup
    {
        private IContainer ApplicationContainer { get; set; }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            DatabaseSessionContext.SetContext(new AsyncLocalDatabaseSessionContext());

            DefaultTypeMap.MatchNamesWithUnderscores = true;

            SqlMapper.AddTypeHandler(typeof(DateTime), new UtcDateTimeHandler());
            SqlMapper.AddTypeHandler(typeof(bool), new BoolHandler());

            services
                .AddMvc()
                .AddJsonOptions(options =>
                {
                    var settings = options.SerializerSettings;
                    if (settings.Converters == null)
                        settings.Converters = new List<JsonConverter>();

                    settings.Converters.Add(new IsoDateTimeConverter());
                })
                .AddControllersAsServices();

            services.AddMetrics(Program.Metrics);
            services.AddMetricsTrackingMiddleware();

            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.RegisterApplicationComponents();

            ApplicationContainer = builder.Build();

            _ = Initialize();

            return new AutofacServiceProvider(ApplicationContainer);
        }

        public void Configure(IApplicationBuilder app,
                              IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMetricsAllMiddleware();
            app.UseRequestLogging();
            app.UseExceptionLogging();
            app.UseOracleSession();
            app.UseMvc();
        }

        private string Initialize()
        {
            try
            {
                using (var session = OracleSession.Create())
                {
                    var connection = session.Connection;

                    return connection.QuerySingle<string>("select dummy from dual");
                }
            }
            catch
            {
                //Ignored
            }

            return null;
        }
    }
}