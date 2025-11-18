using Haley.Enums;
using Haley.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Haley.Abstractions;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace Haley.Utils {
    //https://stackoverflow.com/questions/49694383/use-multiple-jwt-bearer-authentication
    public class AppMaker {
        static AppMaker inst = new AppMaker(); //Singleton
        const string LOCALCORS = "haley_internal_cors";
        const string SWAGGERROUTE = "swaggerroute";
        static AppFlags? _mode;
        public static JWTParameters JWTParams = ResourceUtils.GenerateConfigurationRoot()?.GetSection("Authentication:JWT")?.Get<JWTParameters>();
        AppMakerInput appInput;

        public static AppMaker Get(string[] args, Func<string[]> configPathsProvider = null, AppFlags? mode = null) {
            if (inst.appInput == null) {
                inst.appInput = new AppMakerInput(args, configPathsProvider);
            }
            _mode = mode;
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

        public AppMaker WithFeedbackFilter(bool displayTrace, Func<IActionResult?, Task> customHandler = null) {
            appInput.FeedbackFilterHandler = customHandler;
            appInput.DisplayTraceMessage = displayTrace;
            appInput.AddFeedbackFilter = true;
            return this;
        }

        public AppMaker AddSwaggerRoute(string route) {
            if (string.IsNullOrWhiteSpace(route)) return this;
            appInput.SwaggerRoute = route;
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

        public AppMaker WithCors(bool add_cors = true, Func<string, bool>? originFilter = null, string[]? allowedOrigins = null, bool? reject_invalid_requests = null) {
            appInput.CorsOriginFilter = originFilter; //if origin filter is null
            appInput.AllowedOrigins = allowedOrigins; //if origin filter is null
            appInput.IncludeCors = add_cors;
            appInput.RejectInvalidCorsRequests = reject_invalid_requests;
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

                var sw_sch_name = swgInp.SchemeName;

                if (swgInp.SchemeType == SecuritySchemeType.Http) {
                    if (sw_sch_name != "bearer" && sw_sch_name != "basic") {
                        sw_sch_name = "bearer"; //Resetting the scheme name for HTTP
                    }
                }

                gen.AddSecurityDefinition(swgInp.SchemeName, new OpenApiSecurityScheme {
                    Name = swgInp.HeaderName,
                    Description = $@"Please provide a JWT Token for policy : {swgInp.SchemeName}",
                    In = ParameterLocation.Header,
                    Type = swgInp.SchemeType,
                    BearerFormat = "JWT",
                    Scheme = sw_sch_name
                });

                osr.Add(new OpenApiSecurityScheme {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = swgInp.SchemeName },
                    In = ParameterLocation.Header
                },new string[] { });
            }

            //gen.OperationFilter<IOperationFilter> //Add implementation of a custom filter. Or use the below security requirement
            gen.AddSecurityRequirement(osr);
        }

        static void InitiateSwagger(WebApplication app,string swaggerRoute) {
            var swgRoute = swaggerRoute;
            if (string.IsNullOrWhiteSpace(swgRoute)) {
                try {
                    //user didn't provide any kind of override. Let us try to check if the variable contains any swagger route.
                    //var root = ResourceUtils.GenerateConfigurationRoot();
                    var swgrFb = ResourceUtils.FetchVariable(SWAGGERROUTE);
                    if (!string.IsNullOrWhiteSpace(swgrFb?.Result?.ToString())) swgRoute = swgrFb?.Result?.ToString();
                  
                } catch (Exception ex) {
                    Console.WriteLine($@"Exception while trying to load the swagger route.");
                }
            }
            if (string.IsNullOrWhiteSpace(swgRoute)) {
                app.UseSwagger();
                app.UseSwaggerUI();
                return; ///Return
            }

            app.UseSwagger(c =>
            {
                //c.RouteTemplate = swaggerRoute + "/{documentName}/swagger.json";
                c.RouteTemplate =  "swagger/{documentName}/swagger.json";
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) => {

                    //sometimes httpReq.Scheme might not correctly detect HTTPS, especially behind reverse proxies (like Nginx or Apache). Instead, it sees the internal request as HTTP. So, let us first try to see if we are receiving any xforwarded headers. and then fall back to http.

                    var scheme = httpReq.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? httpReq.Scheme;
                    swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer {
                        Url = $"{scheme}://{httpReq.Host.Value}/{swgRoute}" }};
                });
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"v1/swagger.json", $"API");
                c.RoutePrefix = "swagger";
            });
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

                var cfgRoot = ResourceUtils.GenerateConfigurationRoot();


                if (input.DBGateway is AdapterGateway dbs) {
                    dbs.SetConfigurationRoot(allpaths?.ToArray()).Configure().SetServiceUtil(new DBAServiceUtil());
                    dbs.Updated += ()=> { JWTParams = cfgRoot?.GetSection("Authentication:JWT")?.Get<JWTParameters>(); };
                }

                builder.Services.AddSingleton<IAdapterGateway>(input.DBGateway);
                if (input.AddFeedbackFilter) {
                    var fbFilterArgs = new FeedbackActionArgs();
                    fbFilterArgs.DisplayTraceMessage = input.DisplayTraceMessage;
                    fbFilterArgs.Handler = input.FeedbackFilterHandler;
                    builder.Services.AddSingleton(fbFilterArgs);
                }

                if (input.DBGateway is IModularGateway mg) {
                    builder.Services.AddSingleton<IModularGateway>(mg);
                }

                //ADD BASIC SERVICES
                builder.Services
                    .AddControllers(o => {
                        if (input.AddFeedbackFilter) o.Filters.Add<FeedbackActionFilter>();
                    })
                    .AddJsonOptions(o=> 
                    { o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); 
                    });
                builder.Services.AddEndpointsApiExplorer(); //Which registers all the endpoints

                //ADD SWAGGER (only if it is not disabled by user)
                if (!input.DisableSwagger && (builder.Environment.IsDevelopment() || input.IncludeSwaggerInProduction)) {
                    builder.Services.AddSwaggerGen(gen => GenerateSwagger(gen, input.SwaggerSchemes));
                }

                //ADD AUTHENTICATION AND AUTHORIZATION
                if (input.IncludeDefaultJWTAuth && JWTParams != null) {
                    builder.Services.AddAuthentication(p => {
                        p.DefaultAuthenticateScheme = BaseSchemeNames.HEADER_BEARER_JWT;
                        p.DefaultChallengeScheme = BaseSchemeNames.HEADER_BEARER_JWT;
                    }).AddJwtBearerScheme(BaseSchemeNames.HEADER_BEARER_JWT, JWTUtilEx.ConfigureDefaultJWTAuth);
                    builder.Services.AddAuthorization();
                }

                //CORS (REMEMBER: CORS IS ENFORCED ONLY BY THE BROWSERS and NOT BY THE SERVERS).. So, it is useless to add CORS if your clients are not browsers. 
                //CORS doesn't stop postman, CURL, .net client, bots, or other non-browser clients.
                if (input.IncludeCors) {
                    var corsInfo = cfgRoot["CorsInfo"];
                    Console.WriteLine($@"CorsInfo : {corsInfo?.ToString()}");
                    if (!string.IsNullOrEmpty(corsInfo)) {
                        var corsInfoDic = corsInfo.ToDictionarySplit('&');
                        //Some CORS information is present in the appsettings.. We need to give second priority to this.
                        if (corsInfoDic.TryGetValue("origins",out var corsOrigins) && !string.IsNullOrWhiteSpace(corsOrigins?.ToString()) && (input.AllowedOrigins == null || input.AllowedOrigins.Length < 1)) {
                            //Allowed origins is not present, so, we go ahead and try to set it from the appsettings.
                            var origins = corsOrigins.ToString().CleanSplit(';');
                            input.AllowedOrigins = origins;
                        }

                        if (corsInfoDic.TryGetValue("strict", out var strictEnforce) && input.RejectInvalidCorsRequests == null) input.RejectInvalidCorsRequests = true; //If strict is present then it means, its true.
                    }

                    Console.WriteLine($@"Reject Invalid Cors : {input.RejectInvalidCorsRequests}");

                    builder.Services.AddCors(o => o.AddPolicy(LOCALCORS, b => {
                        b.AllowAnyMethod()
                            .AllowAnyHeader()
                            //.AllowAnyOrigin() //Not working with latest .NET 8+
                            .SetIsOriginAllowed(origin => {

                                if (_mode != null && _mode.ConsoleLog) {
                                    Console.WriteLine($"Incoming Origin: {origin}");
                                    Console.WriteLine("Allowed Origins:");
                                    
                                    foreach (var o in input.AllowedOrigins ?? Array.Empty<string>()) {
                                        Console.WriteLine($"  > {o}");
                                    }
                                }

                                //Level 1 : Check if the origin is in the allowed origins list.
                                if (input.AllowedOrigins != null && input.AllowedOrigins.Length > 0 && input.AllowedOrigins.Contains(origin,StringComparer.OrdinalIgnoreCase)) return true; // no need to check further.

                                //Level 2 : Check with the user defined filter.
                                if (input.CorsOriginFilter != null) return input.CorsOriginFilter.Invoke(origin);

                                //Level 3: No restrictions.
                                if (input.AllowedOrigins == null && input.CorsOriginFilter == null) return true; //No restrictions.

                                if (_mode != null && _mode.ConsoleLog) Console.WriteLine($@"CORS : Origin {origin} not allowed");

                                return false;
                                })
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

                //Cors should always be called before useauthentication, useauthorization and mapcontrollers.
                if (input.IncludeCors) {
                    //Validator middle ware should come before UseCors.
                    if (input.RejectInvalidCorsRequests != null && input.RejectInvalidCorsRequests.Value && input.AllowedOrigins != null && input.AllowedOrigins.Length > 0) {
                        app.UseMiddleware<CorsOriginValidatorMiddleWare>(input.AllowedOrigins);
                    }
                    app.UseCors(LOCALCORS);
                }
                    // INVOKE USER DEFINED SERVICE USES FOR THE APP
                    input.AppProcessor?.Invoke(app);

                if (!input.DisableSwagger && (builder.Environment.IsDevelopment() || input.IncludeSwaggerInProduction)) InitiateSwagger(app,input.SwaggerRoute);

                if (input.HttpsRedirection) {
                    app.UseHttpsRedirection();
                }

                //UseAuth should go before MapControllers and after UseRouting (if applicable)
                if (input.UseAuthentication) app.UseAuthentication();
                if (input.UseAuthorization) app.UseAuthorization();
                app.MapControllers();
                return app;
            } catch (Exception ex) {
                throw new ArgumentException($@"Unable to generate the WebApplication.{Environment.NewLine}Error : {ex.Message}{Environment.NewLine}Trace : {ex.StackTrace}");
            }
          
        }

        private static void Dbs_Updated() {
            throw new NotImplementedException();
        }
    }
}