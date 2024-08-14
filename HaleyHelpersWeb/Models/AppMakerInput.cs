using Haley.Enums;
using Microsoft.AspNetCore.Server.HttpSys;

namespace Haley.Models {
    public class AppMakerInput {
        internal string[] Args { get; set; }
        internal Action<WebApplicationBuilder> BuilderProcessor { get; private set; }
        internal Action<WebApplication> AppProcessor { get; private set; }
        internal Func<string[]> JsonPathsProvider { get; private set; }
        internal Func<string, bool> CorsOriginFilter { get; private set; }
        internal bool IncludeSwaggerInProduction { get; private set; }
        internal bool IncludeJWTAuthentication { get; private set; }
        internal bool HttpsRedirection { get; private set; } = true;
        internal bool AddForwardedHeaders { get; private set; } = true;
        internal bool IncludeCors { get; private set; }

        public AppMakerInput AddDefaultJWTAuth() {
            IncludeJWTAuthentication = true;
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
        public AppMakerInput WithCors(bool add_cors = true,Func<string, bool> originFilter = null) {
            CorsOriginFilter = originFilter; //if origin filter is null
            IncludeCors = add_cors;
            return this;
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