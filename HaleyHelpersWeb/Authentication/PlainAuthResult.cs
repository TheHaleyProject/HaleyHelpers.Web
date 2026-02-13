using System.Security.Claims;

namespace Haley.Models {
    public class PlainAuthResult :Feedback<List<Claim>> {
        public ClaimsPrincipal? Principal { get; set; }
    }
}
