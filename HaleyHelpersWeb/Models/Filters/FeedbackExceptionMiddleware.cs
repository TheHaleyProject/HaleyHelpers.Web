using Haley.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Haley.Models {
    public class FeedbackExceptionMiddleware  {
        RequestDelegate _next;
        bool _isDevelopment;
        ILogger _logger;
        public FeedbackExceptionMiddleware(RequestDelegate next, bool isDev, ILoggerProvider loggerProvider) {
            _next = next;
            _isDevelopment = isDev;
            _logger = loggerProvider?.CreateLogger("Exception Handler");
        }

        public async Task InvokeAsync(HttpContext context) {
            try {
                await _next(context);
            } catch (Exception ex) {
                var feedback = new Feedback {
                    Message = "Unhandled exception occurred.",
                    Trace = _isDevelopment ? ex.ToString() : null,
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

                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(feedback);
            }
        }
    }
}
