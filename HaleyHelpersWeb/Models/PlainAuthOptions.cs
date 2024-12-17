using Haley.Enums;
using Microsoft.AspNetCore.Authentication;
namespace Haley.Models {
    public class PlainAuthOptions : AuthenticationSchemeOptions {
        public string Name { get; set; } = "PlainAuth";
        public Func<HttpContext,string,PlainAuthResult>? Validator { get; set; }
        public PlainAuthMode AuthMode { get; set; } = PlainAuthMode.Basic;
    }
}
