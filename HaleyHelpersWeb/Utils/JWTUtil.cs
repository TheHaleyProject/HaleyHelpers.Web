using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;

namespace Haley.Utils {

    public static class JWTUtil {

        public static string GenerateToken(byte[] key, JwtPayload payload) {
            var securityKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var secToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(secToken);
        }

        public static async Task<JwtSecurityToken> ParseToken(this HttpContext input) {
            try {
                var accessToken = await input.GetTokenAsync("access_token"); //this will work only if we have set the "savetoken=true" in the jWT Bearer settings.
                return new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            } catch (Exception ex) {
                throw new ArgumentException("Exception while trying to fetch the claims from the JWT token. Ensure the JWTBearerOptions has SaveToken set to true.");
            }
        }
    }
}