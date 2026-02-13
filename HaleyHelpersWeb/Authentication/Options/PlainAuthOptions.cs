using Haley.Enums;
using Microsoft.AspNetCore.Authentication;
namespace Haley.Models {
    public class PlainAuthOptions : AuthenticationSchemeOptions {
        public string Key { get; set; } = "Token";
        public Func<HttpContext,string /* token */,ILogger,Task<PlainAuthResult>>? PrepareClaims { get; set; }
        public PlainAuthOptions() { }
    }
}
