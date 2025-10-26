using Haley.Abstractions;

namespace Haley.Models {
    public class MultipartFileInfo {
        public IOSSWrite Request { get; set; }
        public MultipartDataInfo DataInfo { get; set; }
        public string ContentDispositionKey { get; set; }
    }
}