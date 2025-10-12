using Haley.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
namespace Haley.Models {
    public class JwtAuthOptions : PlainAuthOptions {
        //public JWTParameters Params { get; set; }
        public TokenValidationParameters ValidationParams { get; set; }
        public JwtAuthOptions() { base.Key = "Bearer "; }
    }
}
