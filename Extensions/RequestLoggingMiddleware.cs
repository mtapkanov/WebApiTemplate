using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Commons.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace $safeprojectname$.Extensions
{
    public static class RequestLoggingMiddlewareExtensions
    {
        public static void UseRequestLogging(this IApplicationBuilder app)
        {
            app.UseMiddleware<RequestLoggingMiddleware>();
        }
    }

    public class RequestLoggingMiddleware
    {
        private static readonly ILogger Logger = LoggerFactory.Create<RequestLoggingMiddleware>();

        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context)
        {
            if (!Logger.IsDebugEnabled)
            {
                await _next(context);
                return;
            }

            var requestMetadata = RequestMetadata.Create(context.Request);

            Logger.Debug("Received request {@Request}", requestMetadata);

            var originalBodyStream = context.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                await _next(context).ConfigureAwait(false);

                var responseMetadata = ResponseMetadata.Create(context.Response);

                Logger.Debug("Sending response {@Response}", responseMetadata);

                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        internal class RequestMetadata
        {
            public string Uri { get; set; }

            public string Method { get; set; }

            public IDictionary<string, string[]> Headers { get; set; }

            public string Body { get; set; }

            public static RequestMetadata Create(HttpRequest request)
            {
                var result = new RequestMetadata
                {
                    Uri = request.GetDisplayUrl(),
                    Method = request.Method,
                    Headers = request.Headers?.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ToArray()
                    )
                };

                var body = request.Body;

                if (body == null)
                    return result;

                var streamCopy = new MemoryStream();

                request.Body.CopyTo(streamCopy);
                streamCopy.Seek(0, SeekOrigin.Begin);

                result.Body = new StreamReader(streamCopy).ReadToEnd();

                streamCopy.Seek(0, SeekOrigin.Begin);
                request.Body = streamCopy;

                return result;
            }
        }

        internal class ResponseMetadata
        {
            public int StatusCode { get; set; }

            public string Body { get; set; }

            public static ResponseMetadata Create(HttpResponse response)
            {
                var result = new ResponseMetadata
                {
                    StatusCode = response.StatusCode
                };

                if (response.Body == null)
                    return result;

                response.Body.Seek(0, SeekOrigin.Begin);
                result.Body = new StreamReader(response.Body).ReadToEnd();
                response.Body.Seek(0, SeekOrigin.Begin);

                return result;
            }
        }
    }
}