using Haley.Enums;
using Haley.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Haley.Utils {

    public static class WebAppMaker {

        #region Utils
        public static object GetFirst(this object input, ResultFilter filter = ResultFilter.FirstDictionaryValue) {
            //If we send error
            if (input is DBAError dbaerr) {
                return new BadRequestObjectResult(dbaerr.ToString());
            }

            if (input is DBAResult dbres) {
                return new OkObjectResult(dbres.ToString());
            }

            //If we send direct action result
            if (typeof(IActionResult).IsAssignableFrom(input.GetType())) return input;

            if (filter == ResultFilter.None) return input; //Return result as is.

            if (input is List<Dictionary<string, object>> dicList && dicList.Count() > 0) {

                switch (filter) {
                    case ResultFilter.FullList:
                    return dicList;
                    case ResultFilter.FullListValues:
                    return dicList.SelectMany(p => p.Values.Select(q=>q)).ToList();
                    case ResultFilter.FullListValueArray:
                    return dicList.Select(p => p.Values.ToList()).ToList();
                    case ResultFilter.FirstDictionary:
                    return dicList.First();
                    case ResultFilter.FirstDictionaryValue:
                    if (dicList.First()?.First() != null) return dicList.First().First().Value;
                    return dicList.First().First();
                }
            }
            return input;
        }

        public static async Task<object> GetFirst(this Task<object> input) {
            var inpuobj = await input;
            return inpuobj?.GetFirst();
        }

        #endregion

        public static JWTParameters JWTParams = Globals.JWTParams;
        public static WebApplication GetAppWithJWT(string[] args, Action<WebApplicationBuilder> builderProcessor = null, Action<WebApplication> appProcessor = null, Func<string[]> jsonPathsProvider = null, bool swagger_in_production = false) {
            return GetAppInternal(WebAppAuthMode.JWT,args,builderProcessor, appProcessor,jsonPathsProvider, swagger_in_production);
        }

        public static WebApplication GetApp(string[] args, Action<WebApplicationBuilder> builderProcessor = null, Action<WebApplication> appProcessor = null, Func<string[]> jsonPathsProvider = null, bool swagger_in_production = false) {
            return GetAppInternal(WebAppAuthMode.None, args, builderProcessor, appProcessor, jsonPathsProvider, swagger_in_production);
        }

        static WebApplication GetAppInternal(WebAppAuthMode mode , string[] args, Action<WebApplicationBuilder> builderProcessor = null, Action<WebApplication> appProcessor = null, Func<string[]> jsonPathsProvider = null, bool swagger_in_production = false) {

            try {
                //SETUP THE DB ADAPTER DICTIONARY
                var builder = WebApplication.CreateBuilder(args);
                List<string> allpaths = new List<string>(); //Json paths.
                if (jsonPathsProvider != null) {
                    var jpaths = jsonPathsProvider.Invoke();
                    if (jpaths != null && jpaths.Count() > 0) {
                        allpaths.AddRange(jpaths.Select(q => q.ToLower().Trim()));
                    }
                }

                if (allpaths != null && allpaths.Count > 0) {
                    allpaths = allpaths.Distinct().ToList(); //Remove duplicates
                }

                DBAService.Instance.SetConfigurationRoot(allpaths?.ToArray()).Configure().SetServiceUtil(new DBAServiceUtil());
                DBAService.Instance.Updated += Globals.HandleConfigUpdate;

                builder.Services.AddSingleton(DBAService.Instance); //Not necessary as we can directly call the singleton.

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
            } catch (Exception ex) {
                throw new ArgumentException($@"Unable to generate the WebApplication - {ex.StackTrace}");
            }
          
        }
    }
}