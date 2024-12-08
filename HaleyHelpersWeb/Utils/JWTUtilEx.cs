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
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Haley.Utils {

    public static class JWTUtilEx {

        public static void ConfigureDefaultJWTAuth(JwtBearerOptions options) {
            options.RequireHttpsMetadata = false; //HTTPS not required now.
            options.SaveToken = true;
            var jwtparams = Globals.JWTParams;
            options.TokenValidationParameters = JWTUtil.GenerateTokenValidationParams(jwtparams);
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