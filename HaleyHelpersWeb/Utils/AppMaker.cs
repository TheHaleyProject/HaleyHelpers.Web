using Haley.Enums;
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
using System.Runtime.CompilerServices;

namespace Haley.Utils {
    //https://stackoverflow.com/questions/49694383/use-multiple-jwt-bearer-authentication
    public class AppMaker {
        static AppMaker inst = new AppMaker(); //Singleton
        const string LOCALCORS = "localCors";
        public static JWTParameters JWTParams = Globals.JWTParams;
        AppMakerInput appInput;

        public static AppMaker Get(string[] args, Func<string[]> configPathsProvider = null) {
            if (inst.appInput == null) {
                inst.appInput = new AppMakerInput(args, configPathsProvider);
            }
            return inst;
        }

        public WebApplication Build() {
            if (inst.appInput == null) inst.appInput = new AppMakerInput(null, null); //Generate a dummy appmaker input without any parameters.
            return GetAppInternal(inst.appInput);
        }

        public AppMaker UseAuth(bool use_authentication = false, bool use_authorization = false) {
            appInput.UseAuthentication = use_authentication;
            appInput.UseAuthorization = use_authorization;
            return this;
        }

        public AppMaker AddSwaggerScheme(SwaggerInput input) {
            if (!string.IsNullOrWhiteSpace(input.SchemeName) && !appInput.SwaggerSchemes.Any(p => p.SchemeName == input.SchemeName)) {
                appInput.SwaggerSchemes.Add(input);
            }
            return this;
        }

        public AppMaker AddDefaultJWTAuth() {
            appInput.IncludeDefaultJWTAuth = true;
            return this;
        }
        public AppMaker AddSwaggerinProduction(bool add_swagger = true) {
            appInput.IncludeSwaggerInProduction = add_swagger;
            return this;
        }

        public AppMaker WithForwardedHeaders(bool forward_headers = true) {
            appInput.AddForwardedHeaders = forward_headers;
            return this;
        }

        public AppMaker WithoutSwagger() {
            appInput.DisableSwagger = true;
            return this;
        }
        public AppMaker WithHttpsRedirection(bool https_redir = true) {
            appInput.HttpsRedirection = https_redir;
            return this;
        }
        public AppMaker WithCors(bool add_cors = true, Func<string, bool> originFilter = null) {
            appInput.CorsOriginFilter = originFilter; //if origin filter is null
            appInput.IncludeCors = add_cors;
            return this;
        }

        public AppMaker WithService (IAdapterGateway data_gateway) {
            if (data_gateway == null) {
                appInput.DBGateway = new AdapterGateway(true); //Let us autoconfigure it.
            } else {
                appInput.DBGateway = data_gateway;
            }
            return this;
        }

        public AppMaker ExposeHeaders(params string[] headers) {
            if (headers == null || headers.Count() < 1) return this;
            if (appInput.ExposedHeaders == null) appInput.ExposedHeaders = new List<string>();
            foreach (var header in headers) {
                if (string.IsNullOrWhiteSpace(header)) continue;
                appInput.ExposedHeaders.Add(header);
            }
            return this;
        }

        public AppMaker ClearExposedHeaders() {
            appInput.ExposedHeaders?.Clear(); return this;
        }

        public AppMaker WithAppProcessor(Action<WebApplication> app) {
            if (appInput.AppProcessor == null) appInput.AppProcessor = app;
            return this;
        }
        public AppMaker WithBuilderProcessor(Action<WebApplicationBuilder> builder) {
            if (appInput.BuilderProcessor == null) appInput.BuilderProcessor = builder;
            return this;
        }

        static void GenerateSwagger(SwaggerGenOptions gen, List<SwaggerInput> swaggerInputs) {
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

                Globals.DBService = input.DBGateway; 

                if (input.DBGateway is AdapterGateway dbs) {
                    dbs.SetConfigurationRoot(allpaths?.ToArray()).Configure().SetServiceUtil(new DBAServiceUtil());
                    dbs.Updated += Globals.HandleConfigUpdate;
                }

                builder.Services.AddSingleton<IAdapterGateway>(input.DBGateway);
                if (input.DBGateway is IModularGateway mg) {
                    builder.Services.AddSingleton<IModularGateway>(mg);
                }

                //ADD BASIC SERVICES
                builder.Services.AddControllers().AddJsonOptions(o=> { o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
                builder.Services.AddEndpointsApiExplorer(); //Which registers all the endpoints

                //ADD SWAGGER (only if it is not disabled by user)
                if (!input.DisableSwagger && (builder.Environment.IsDevelopment() || input.IncludeSwaggerInProduction)) {
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

                if (!input.DisableSwagger && (builder.Environment.IsDevelopment() || input.IncludeSwaggerInProduction)) {
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