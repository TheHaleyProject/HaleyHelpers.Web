using Haley.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Haley.Models {
    public class CorsOriginValidatorMiddleWare {
        private readonly RequestDelegate _next;
        private readonly CorsInfo _info;
        public CorsOriginValidatorMiddleWare(RequestDelegate next, CorsInfo info) {
            _next = next;
            _info = info;
        }

        public async Task InvokeAsync(HttpContext context) {
            if (_info == null || _info.AllowedOrigins == null || _info.AllowedOrigins.Length < 1) {
                await _next(context);
                return;
            }

            var origin = context.Request.Headers["Origin"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(origin) && !_info.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)) {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync($"Middleware: CORS: Origin {origin} is not allowed.");
                return;
            }

            await _next(context);
        }
    }
}
