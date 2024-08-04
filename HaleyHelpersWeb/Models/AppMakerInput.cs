using Haley.Enums;

namespace Haley.Models {
    public class AppMakerInput {
        public string[] Args { get; set; }
        public Action<WebApplicationBuilder> BuilderProcessor { get; set; }
        public Action<WebApplication> AppProcessor { get; set; }
        public Func<string[]> JsonPathsProvider { get; set; }
        public bool IncludeSwaggerInProduction { get; set; }
        public bool HttpsRedirection { get; set; } = true;
        public List<WebAppAuthMode> AuthModes { get; set; } = new List<WebAppAuthMode>();
        public bool UseForwardedHeaders { get; set; } = true;
        public bool IncludeCors { get; set; }
        public Func<string,bool> CorsOriginFilter { get; set; }
        public AppMakerInput(string[] args, Action<WebApplicationBuilder> builder, Action<WebApplication> app ) { Args = args; BuilderProcessor = builder; AppProcessor = app; }
        public AppMakerInput(string[] args, Action<WebApplicationBuilder> builder):this(args,builder,null) {  }
        public AppMakerInput(string[] args, Action<WebApplication> app):this(args,null,app) {  }
        public AppMakerInput(string[] args):this(args, null,null) {
        }
    }
}
