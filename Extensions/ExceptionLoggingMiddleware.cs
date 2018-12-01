using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Commons.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace $safeprojectname$.Extensions
{
    public static class ExceptionLoggingMiddlewareExtensions
    {
        public static void UseExceptionLogging(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionLoggingMiddleware>();
        }
    }

    public class ExceptionLoggingMiddleware
    {
        private static readonly ILogger Logger = LoggerFactory.Create<ExceptionLoggingMiddleware>();

        private readonly RequestDelegate _next;

        public ExceptionLoggingMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unhandled exception on {@Request}", new RequestMetadata(context.Request));

                throw;
            }
        }

        internal class RequestMetadata
        {
            public RequestMetadata(HttpRequest request)
            {
                QueryString = request.GetDisplayUrl();
                Method = request.Method;
                Headers = request.Headers?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToArray()
                );
            }

            public string QueryString { get; }

            public string Method { get; }

            public IDictionary<string, string[]> Headers { get; }
        }
    }
}