using Haley.Enums;
using Microsoft.AspNetCore.Authentication;
namespace Haley.Models {
    public class PlainAuthOptions : AuthenticationSchemeOptions {
        public string Name { get; set; } = "PlainAuth";
        public Func<HttpContext,string,ILogger,Task<PlainAuthResult>>? Validator { get; set; }
    }
}
