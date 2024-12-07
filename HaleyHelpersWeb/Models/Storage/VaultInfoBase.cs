using Microsoft.AspNetCore.Mvc;

namespace Haley.Models {

    public class VaultInfoBase {

        [FromQuery(Name = "bn")]
        public string? BucketName { get; set; } //Sometimes we dont' want any root dir to be specified. We direclty start uploading.

        [FromQuery(Name = "ubid")]
        public long? UnmanagedBucketId { get; set; } //Sometimes we dont' want any root dir to be specified. We direclty start uploading.

        public VaultInfoBase() {
        }
    }
}