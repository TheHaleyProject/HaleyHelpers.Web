using Microsoft.OpenApi.Models;

namespace Haley.Models {
    public class SwaggerInput {
        public SecuritySchemeType SchemeType { get; set; }
        public string SchemeName { get; set; }
        public string HeaderName { get; set; }
        public SwaggerInput(string schemeName, string headerName, SecuritySchemeType schemeType) {
            SchemeName = schemeName;
            HeaderName = headerName;
            SchemeType = schemeType;
        }
    }
}
