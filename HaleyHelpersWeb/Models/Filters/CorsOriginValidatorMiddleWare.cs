using Haley.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Haley.Models {
    public class CorsOriginValidatorMiddleWare {
        private readonly RequestDelegate _next;
        private readonly string[] _allowedOrigins;
        public CorsOriginValidatorMiddleWare(RequestDelegate next, string[] allowedOrigins) {
            _next = next;
            _allowedOrigins = allowedOrigins ?? Array.Empty<string>();
        }

        public async Task InvokeAsync(HttpContext context) {
            if (_allowedOrigins == null || _allowedOrigins.Length < 1) {
                await _next(context);
                return;
            }

            var origin = context.Request.Headers["Origin"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(origin) && !_allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)) {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync($"Middleware: CORS: Origin {origin} is not allowed.");
                return;
            }

            await _next(context);
        }
    }
}
