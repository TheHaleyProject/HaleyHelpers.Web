using Haley.Enums;
using Haley.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Haley.Abstractions;

namespace Haley.Utils {

    public static class WebAppMaker {
        const string LOCALCORS = "localCors";

        #region Utils
        public static object ConvertDBAResult(this object input, ResultFilter filter = ResultFilter.FirstDictionaryValue) {
            //If we send error
            if (input is DBAError dbaerr) {
                return new BadRequestObjectResult(dbaerr.ToString());
            }
            if (input is DBAResult dbres) {
                return new OkObjectResult(dbres.ToString());
            }
            //If we send direct action result
            if (typeof(IActionResult).IsAssignableFrom(input.GetType())) return input;
            return input;
        }

        #endregion

        public static JWTParameters JWTParams = Globals.JWTParams;

        public static WebApplication GetApp(AppMakerInput input) {
            if (input == null) return null;
            return GetAppInternal(input);
        }

        static WebApplication GetAppInternal(AppMakerInput input) {

            try {
                //SETUP THE DB ADAPTER DICTIONARY
                var builder = WebApplication.CreateBuilder(input.Args);
                List<string> allpaths = new List<string>(); //Json paths.
                if (input.JsonPathsProvider != null) {
                    var jpaths = input.JsonPathsProvider.Invoke();
                    if (jpaths != null && jpaths.Count() > 0) {
                        allpaths.AddRange(jpaths.Select(q => q.ToLower().Trim()));
                    }
                }

                if (allpaths != null && allpaths.Count > 0) {
                    allpaths = allpaths.Distinct().ToList(); //Remove duplicates
                }

                DBAService.Instance.SetConfigurationRoot(allpaths?.ToArray()).Configure().SetServiceUtil(new DBAServiceUtil());
                DBAService.Instance.Updated += Globals.HandleConfigUpdate;

                builder.Services.AddSingleton<IDBService,DBAService>(provider => DBAService.Instance ); //Not necessary as we can directly call the singleton.

                //ADD BASIC SERVICES
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                if (builder.Environment.IsDevelopment() || input.IncludeSwaggerInProduction) {
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
                if (input.AuthMode == WebAppAuthMode.JWT && Globals.JWTParams != null) {
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

                if (input.AuthMode != WebAppAuthMode.None) {
                    builder.Services.AddAuthorization();
                }

                //CORS
                if (input.IncludeCors) {
                    builder.Services.AddCors(o => o.AddPolicy(LOCALCORS, b => {
                        b.AllowAnyMethod()
                            .AllowAnyHeader()
                            //.AllowAnyOrigin() //Not working with latest .NET 8+
                            .SetIsOriginAllowed(origin => input.OriginFilter == null ? true : input.OriginFilter.Invoke(origin))
                            //.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost") //Allow local host.
                            .AllowCredentials()
                            .WithExposedHeaders("Content-Disposition"); // params string[]
                    }));
                }

                //INVOKE USER DEFINED SERVICE ADDITION
                input.BuilderProcessor?.Invoke(builder);
                //builder.Logging.ClearProviders(); //only for production.

                var app = builder.Build();

                if (input.IncludeCors) {
                    app.UseCors(LOCALCORS);
                }
                    // INVOKE USER DEFINED SERVICE USES FOR THE APP
                    input.AppProcessor?.Invoke(app);

                if (builder.Environment.IsDevelopment() || input.IncludeSwaggerInProduction) {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                if (input.HttpsRedirection) {
                    app.UseHttpsRedirection();
                }

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