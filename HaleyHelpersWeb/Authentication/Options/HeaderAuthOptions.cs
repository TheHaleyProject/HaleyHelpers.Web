using Haley.Enums;
using Microsoft.AspNetCore.Authentication;
namespace Haley.Models {
    public class HeaderAuthOptions : PlainAuthOptions {
        public HeaderAuthOptions() { base.Key = "Bearer"; }
    }
}
