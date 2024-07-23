using Microsoft.AspNetCore.Mvc;

namespace Haley.Models {
    public class MultipartUploadSummary : ObjectCreateSummary {
        public bool DataHandled { get; set; }
    }
}
