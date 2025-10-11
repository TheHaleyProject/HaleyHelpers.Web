using Haley.Enums;
using Microsoft.AspNetCore.Authentication;
namespace Haley.Models {
    public class PlainAuthOptions : AuthenticationSchemeOptions {
        public string Key { get; set; } = "Bearer ";
        public Func<HttpContext,string,ILogger,Task<PlainAuthResult>>? Validator { get; set; }
        public PlainAuthOptions() { }
    }
}
