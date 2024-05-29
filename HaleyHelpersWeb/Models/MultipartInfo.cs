using Microsoft.AspNetCore.Mvc;

namespace Haley.Models {
    public class MultipartInfo {
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public Stream Stream { get; set; }
    }
}
