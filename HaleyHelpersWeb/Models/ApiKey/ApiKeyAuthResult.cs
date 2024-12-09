using System.Security.Claims;

namespace Haley.Models {
    public class ApiKeyAuthResult {
        public bool Status { get; set; }
        public List<Claim> Claims { get; set; } = new List<Claim>();
    }
}
