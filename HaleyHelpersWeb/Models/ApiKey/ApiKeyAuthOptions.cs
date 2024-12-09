using Microsoft.AspNetCore.Authentication;
namespace Haley.Models {
    public class ApiKeyAuthOptions : AuthenticationSchemeOptions {
        public string Header { get; set; } = "ApiKey";
        public Func<string,ApiKeyAuthResult> Validator { get; set; }
    }
}
