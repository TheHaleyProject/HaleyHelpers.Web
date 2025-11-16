using Microsoft.AspNetCore.Authorization;

namespace Haley.Models {
    public class AuthPolicyMaker {
        public string[] SchemeNames { get; set; }
        public string[] RoleNames { get; set; }
        public string[] ClaimNames { get; set; }
        public string PolicyName { get; set; }
        public AuthorizationOptions Options { get; set; }
    }
}
