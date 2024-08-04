using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Client;
using Haley.Models;
using Haley.Utils;
using Microsoft.AspNetCore.DataProtection;
using System.Text;
using Azure.Core;

namespace Haley.Utils {

    public static class JWTUtil {

        public static string GenerateToken(JwtPayload payload) {
            var key = HashUtils.GetRandomBytes(256);
            return GenerateToken(key.bytes,payload);
        }

        public static string GenerateToken(string key,JwtPayload payload) {
            var _secret = key;
            if (key.IsBase64()) {
                _secret = Encoding.UTF8.GetString(Convert.FromBase64String(key));
            }
            return GenerateToken(Encoding.ASCII.GetBytes(_secret), payload);
        }

        public static string GenerateToken(byte[] key, JwtPayload payload) {
            var securityKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var secToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(secToken);
        }

        public static async Task<JwtSecurityToken> GetJwtToken(this HttpContext input,string key= "access_token") {
            try {
                var accessToken = await input.GetTokenAsync(key); //this will work only if we have set the "savetoken=true" in the jWT Bearer settings.
                return new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            } catch (Exception ex) {
                return null;
            }
        }

        public static async Task<string> GetJwtClaim(this HttpContext context,string claimName, string key = "access_token") {
            var jwt = await context.GetJwtToken(key);
            if (jwt == null) throw new ArgumentNullException("JWT token is null. Hint: Add token in authorization or Ensure the JWTBearerOptions has SaveToken set to true.");
            return jwt?.Claims?.FirstOrDefault(p => p.Type == claimName)?.Value ?? null;
        }


        public static async Task<string> GetDBA(this HttpContext context) {
            return await context.GetJwtClaim(JWTClaimType.DBA_KEY);
        }
    }
}