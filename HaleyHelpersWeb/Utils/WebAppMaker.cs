using Haley.Models;
using Microsoft.OpenApi.Models;
using System.Drawing;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Haley.Enums;

namespace Haley.Utils {

    public static class WebAppMaker {

        public static JWTParameters JWTParams = Globals.JWTParams;
        public static WebApplication GetAppWithJWT(string[] args, Action<WebApplicationBuilder> builderProcessor = null, Action<WebApplication> appProcessor = null, Func<string[]> jsonPathsProvider = null, bool swagger_in_production = false) {
            return GetAppInternal(WebAppAuthMode.JWT,args,builderProcessor, appProcessor,jsonPathsProvider, swagger_in_production);
        }

        public static WebApplication GetApp(string[] args, Action<WebApplicationBuilder> builderProcessor = null, Action<WebApplication> appProcessor = null, Func<string[]> jsonPathsProvider = null, bool swagger_in_production = false) {
            return GetAppInternal(WebAppAuthMode.None, args, builderProcessor, appProcessor, jsonPathsProvider, swagger_in_production);
        }

        static WebApplication GetAppInternal(WebAppAuthMode mode , string[] args, Action<WebApplicationBuilder> builderProcessor = null, Action<WebApplication> appProcessor = null, Func<string[]> jsonPathsProvider = null, bool swagger_in_production = false) {

            //SETUP THE DB ADAPTER DICTIONARY
            var builder = WebApplication.CreateBuilder(args);
            List<string> allpaths = new List<string>(); //Json paths.
            if (jsonPathsProvider != null) {
                var jpaths = jsonPathsProvider.Invoke();
                if (jpaths != null && jpaths.Count() > 0) {
                    allpaths.AddRange(jpaths.Select(q => q.ToLower().Trim()));
                }
            }

            allpaths = allpaths.Distinct().ToList(); //Remove duplicates

            DBAdapterDictionary.Instance.SetConfigurationRoot(allpaths.ToArray()).Configure();
            DBAdapterDictionary.Instance.Updated += Globals.HandleConfigUpdate;

            builder.Services.AddSingleton(DBAdapterDictionary.Instance); //Not necessary as we can directly call the singleton.

            //ADD BASIC SERVICES
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            if (builder.Environment.IsDevelopment() || swagger_in_production) {
                builder.Services.AddSwaggerGen(gen => {
                    gen.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
                        Name = "Authorization",
                        Description = "Please provide a JWT Token",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = JwtBearerDefaults.AuthenticationScheme
                    });
                    //gen.OperationFilter<IOperationFilter> //Add implementation of a custom filter. Or use the below security requirement
                    gen.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme {
                            Reference = new OpenApiReference {
                                Type =ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
                });
            }


            //ADD AUTHENTICATION AND AUTHORIZATION
            if (mode == WebAppAuthMode.JWT && Globals.JWTParams != null) {
                builder.Services.AddAuthentication(p => {
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

            if (mode != WebAppAuthMode.None) {
                builder.Services.AddAuthorization();
            }

            //INVOKE USER DEFINED SERVICE ADDITION
            builderProcessor?.Invoke(builder);
            //builder.Logging.ClearProviders(); //only for production.

            var app = builder.Build();

            // INVOKE USER DEFINED SERVICE USES FOR THE APP
            appProcessor?.Invoke(app);

            if (builder.Environment.IsDevelopment() || swagger_in_production) {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            return app;
        }
    }
}