using System.Security.Claims;

namespace Haley.Models {
    public class PlainAuthResult {
        public bool Status { get; set; }
        public List<Claim>? Claims { get; set; } = new List<Claim>();
        public string? Message { get; set; }
    }
}
