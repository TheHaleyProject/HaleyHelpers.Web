using Haley.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Haley.Models {
    public class FeedbackExceptionMiddleware  {
        RequestDelegate _next;
        AppFlags _flags;
        ILogger _logger;
        public FeedbackExceptionMiddleware(RequestDelegate next, ILoggerProvider loggerProvider,AppFlags appFlags) {
            _next = next;
            _flags = appFlags ?? new AppFlags();
            _logger = loggerProvider?.CreateLogger("Exception Handler");
        }

        public async Task InvokeAsync(HttpContext context) {
            try {
                await _next(context);
            } catch (Exception ex) {
                var feedback = new Feedback {
                    Message = "Unhandled exception occurred.",
                    Trace = _flags.Debug? ex.ToString() : null,
                    Status = false
                };
                var sb = new StringBuilder();
                if (context?.Request != null) {
                    sb.AppendLine($@"{Environment.NewLine}Endpoint : {context.Request.Scheme}://{context.Request.Host}{context.Request.Path} {context.Request.Protocol}");
                }
               
                sb.AppendLine($@"Message : {ex.Message}");
                sb.AppendLine($@"Trace : {ex.StackTrace}");
                var err = sb.ToString();
                _logger?.LogError(err);
                Console.WriteLine(err);

                if (context != null) {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(feedback);
                }
            }
        }
    }
}
