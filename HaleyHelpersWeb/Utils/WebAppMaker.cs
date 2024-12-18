﻿using Haley.Enums;
using Haley.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Haley.Abstractions;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Haley.Utils {
    //https://stackoverflow.com/questions/49694383/use-multiple-jwt-bearer-authentication

    public static class WebAppMaker {
        const string LOCALCORS = "localCors";

        public static JWTParameters JWTParams = Globals.JWTParams;

        public static WebApplication GetApp(AppMakerInput input) {
            if (input == null) return null;
            return GetAppInternal(input);
        }

        public static void GenerateSwagger(SwaggerGenOptions gen, List<SwaggerInput> swaggerInputs) {
            //gen.SwaggerDoc(
            // "v1",
            // new OpenApiInfo { Title = "Some title", Version = "v1" });
            gen.OrderActionsBy((apiDesc) => $"{apiDesc.RelativePath}");
            //gen.DescribeAllParametersInCamelCase();

            //Foreach values (add definition & security requirements)
            var osr = new OpenApiSecurityRequirement();

            foreach (var swgInp in swaggerInputs) {
                gen.AddSecurityDefinition(swgInp.SchemeName, new OpenApiSecurityScheme {
                    Name = swgInp.HeaderName,
                    Description = $@"Please provide a JWT Token for policy : {swgInp.SchemeName}",
                    In = ParameterLocation.Header,
                    Type = swgInp.SchemeType,
                    BearerFormat = "JWT",
                    Scheme = swgInp.SchemeName
                });

                osr.Add(new OpenApiSecurityScheme {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = swgInp.SchemeName },
                    In = ParameterLocation.Header
                },new string[] { });
            }

            //gen.OperationFilter<IOperationFilter> //Add implementation of a custom filter. Or use the below security requirement
            gen.AddSecurityRequirement(osr);
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
                builder.Services.AddControllers().AddJsonOptions(o=> { o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
                builder.Services.AddEndpointsApiExplorer();

                //ADD SWAGGER
                if (builder.Environment.IsDevelopment() || input.IncludeSwaggerInProduction) {
                    builder.Services.AddSwaggerGen(gen => GenerateSwagger(gen, input.SwaggerSchemes));
                }

                //ADD AUTHENTICATION AND AUTHORIZATION
                if (input.IncludeDefaultJWTAuth && Globals.JWTParams != null) {
                    builder.Services.AddAuthentication(p => {
                        p.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        p.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    }).AddJwtBearer(JWTUtilEx.ConfigureDefaultJWTAuth);
                    builder.Services.AddAuthorization();
                }

                //CORS
                if (input.IncludeCors) {
                    builder.Services.AddCors(o => o.AddPolicy(LOCALCORS, b => {
                        b.AllowAnyMethod()
                            .AllowAnyHeader()
                            //.AllowAnyOrigin() //Not working with latest .NET 8+
                            .SetIsOriginAllowed(origin => input.CorsOriginFilter == null ? true : input.CorsOriginFilter.Invoke(origin))
                            //.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost") //Allow local host.
                            .AllowCredentials();

                        if (input.ExposedHeaders != null && input.ExposedHeaders.Count > 0) {
                            b.WithExposedHeaders(input.ExposedHeaders.ToArray()); // params string[]
                        }
                    }));
                }

                //HEADERS FORWARD
                if (input.AddForwardedHeaders) {
                    builder.Services.Configure<ForwardedHeadersOptions>(options =>
                    {
                        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                            ForwardedHeaders.XForwardedProto;
                        // Only loopback proxies are allowed by default.
                        // Clear that restriction because forwarders are enabled by explicit 
                        // configuration.
                        options.KnownNetworks.Clear();
                        options.KnownProxies.Clear();
                    });
                }

                //INVOKE USER DEFINED SERVICE ADDITION
                input.BuilderProcessor?.Invoke(builder);
                //builder.Logging.ClearProviders(); //only for production.

                var app = builder.Build();

                if (input.AddForwardedHeaders) {
                    app.UseForwardedHeaders();
                }

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

                //UseAuth should go before MapControllers and after UseRouting (if applicable)
                if (input.UseAuthentication) app.UseAuthentication();
                if (input.UseAuthorization) app.UseAuthorization();
                app.MapControllers();
                return app;
            } catch (Exception ex) {
                throw new ArgumentException($@"Unable to generate the WebApplication - {ex.StackTrace}");
            }
          
        }
    }
}