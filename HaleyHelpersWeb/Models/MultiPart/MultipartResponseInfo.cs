using Microsoft.AspNetCore.Mvc;

namespace Haley.Models {
    public class MultipartResponseInfo {
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public Stream Stream { get; set; }
    }
}
