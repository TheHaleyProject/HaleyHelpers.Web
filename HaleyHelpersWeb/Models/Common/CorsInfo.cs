
namespace Haley.Models {
    public class CorsInfo {
        public Func<string, bool>? CorsOriginFilter { get; set; }
        public string[]? AllowedOrigins { get; set; }
        public bool IncludeCors { get; set; }
        public bool? RejectInvalidCorsRequests { get; set; }
    }
}