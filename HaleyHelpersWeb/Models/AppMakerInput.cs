using Haley.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.OpenApi.Models;
using System.Reflection.Metadata.Ecma335;

namespace Haley.Models {
    public class AppMakerInput {
        internal string[] Args { get; set; }
        internal Action<WebApplicationBuilder> BuilderProcessor { get; private set; }
        internal Action<WebApplication> AppProcessor { get; private set; }
        internal Func<string[]> JsonPathsProvider { get; private set; }
        internal Func<string, bool> CorsOriginFilter { get; private set; }
        internal List<string> ExposedHeaders { get; private set; } = new List<string> { "Content-Disposition" };
        internal bool IncludeSwaggerInProduction { get; private set; }
        internal bool IncludeDefaultJWTAuth { get; private set; } = false;
        internal bool HttpsRedirection { get; private set; } = true;
        internal bool AddForwardedHeaders { get; private set; } = true;
        internal bool IncludeCors { get; private set; }
        internal bool UseAuthentication { get; private set; } = false;
        internal bool UseAuthorization { get; private set; } = false;
        internal List<SwaggerInput> SwaggerSchemes { get; private set; } = new List<SwaggerInput> { new SwaggerInput(JwtBearerDefaults.AuthenticationScheme, "Authorization", SecuritySchemeType.Http) };

        public AppMakerInput UseAuth(bool use_authentication = false, bool use_authorization = false) {
            UseAuthentication = use_authentication;
            UseAuthorization = use_authorization;
            return this;
        }

        public AppMakerInput AddSwaggerScheme(SwaggerInput input) {
            if (!string.IsNullOrWhiteSpace(input.SchemeName) && !SwaggerSchemes.Any(p => p.SchemeName == input.SchemeName)) {
                SwaggerSchemes.Add(input);
            }
            return this;
        }

        public AppMakerInput AddDefaultJWTAuth() {
            IncludeDefaultJWTAuth = true;
            return this;
        }
        public AppMakerInput AddSwaggerinProduction(bool add_swagger = true) {
            IncludeSwaggerInProduction = add_swagger;
            return this;
        }

        public AppMakerInput WithForwardedHeaders(bool forward_headers = true) {
            AddForwardedHeaders = forward_headers;
            return this;
        }
        public AppMakerInput WithHttpsRedirection(bool https_redir = true) {
            HttpsRedirection = https_redir;
            return this;
        }
        public AppMakerInput WithCors(bool add_cors = true, Func<string, bool> originFilter = null) {
            CorsOriginFilter = originFilter; //if origin filter is null
            IncludeCors = add_cors;
            return this;
        }

        public AppMakerInput ExposeHeaders(params string[] headers) {
            if (headers == null || headers.Count() < 1) return this;
            if (ExposedHeaders == null) ExposedHeaders = new List<string>();
            foreach (var header in headers) {
                if (string.IsNullOrWhiteSpace(header)) continue;
                ExposedHeaders.Add(header);
            }
            return this;
        }

        public AppMakerInput ClearExposedHeaders() {
            ExposedHeaders?.Clear(); return this; 
        }

        public AppMakerInput WithAppProcessor(Action<WebApplication> app) {
            if (AppProcessor == null) AppProcessor = app;
            return this;
        }
        public AppMakerInput WithBuilderProcessor(Action<WebApplicationBuilder> builder) {
            if (BuilderProcessor == null) BuilderProcessor = builder; 
            return this;
        }
        public AppMakerInput(string[] args, Func<string[]> configPathsProvider) { Args = args; JsonPathsProvider = configPathsProvider; }
        public AppMakerInput(string[] args) : this(args, null) { }
    }
}