using Haley.Abstractions;
using Haley.Enums;
using Haley.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.OpenApi.Models;
using System.Reflection.Metadata.Ecma335;

namespace Haley.Models {
    internal class AppMakerInput {
        IAdapterGateway _dbs;
        internal IAdapterGateway DBGateway {
            get {
                if (_dbs == null) {
                    _dbs = new AdapterGateway(true);
                }
                return _dbs; }
            set { _dbs = value; }
        }
        internal string[] Args { get; set; }
        internal Action<WebApplicationBuilder> BuilderProcessor { get; set; }
        internal Action<WebApplication> AppProcessor { get; set; }
        internal Func<string[]> JsonPathsProvider { get; set; }
        internal Func<string, bool> CorsOriginFilter { get; set; }
        internal List<string> ExposedHeaders { get; set; } = new List<string> { "Content-Disposition" };
        internal bool IncludeSwaggerInProduction { get; set; }
        internal bool DisableSwagger { get; set; }
        internal bool IncludeDefaultJWTAuth { get; set; } = false;
        internal bool HttpsRedirection { get; set; } = true;
        internal bool AddForwardedHeaders { get; set; } = true;
        internal bool IncludeCors { get; set; }
        internal bool UseAuthentication { get; set; } = false;
        internal bool UseAuthorization { get; set; } = false;
        internal string SwaggerRoute { get; set; }
        internal List<SwaggerInput> SwaggerSchemes { get; set; } = new List<SwaggerInput> { new SwaggerInput(JwtBearerDefaults.AuthenticationScheme, "Authorization", SecuritySchemeType.Http) };
        public AppMakerInput(string[] args, Func<string[]> configPathsProvider) { Args = args; JsonPathsProvider = configPathsProvider; }
        public AppMakerInput(string[] args) : this(args, null) { }
    }
}