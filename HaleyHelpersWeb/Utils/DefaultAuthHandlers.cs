using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Haley.Utils {
    public static class DefaultAuthHandlers {
        public static AuthenticationBuilder AddDefaultJWTAuthentication(this IServiceCollection services) {
           return services.AddAuthentication(p => {
                p.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                p.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(q => {
                q.RequireHttpsMetadata = false; //HTTPS not required now.
                q.SaveToken = true;
                var jwtparams = Globals.JWTParams;
                q.TokenValidationParameters = new TokenValidationParameters() {
                    ValidateIssuerSigningKey = true, //Important as this will verfiy the signature
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RequireExpirationTime = true,
                    ValidateIssuer = jwtparams.ValidateIssuer,
                    ValidateAudience = jwtparams.ValidateAudience,
                    ValidIssuer = jwtparams.Issuer,
                    ValidAudience = jwtparams.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(jwtparams.GetSecret())
                };
            });
        }

    }
}
