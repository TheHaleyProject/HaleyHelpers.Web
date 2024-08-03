using Microsoft.AspNetCore.Mvc;

namespace Haley.Models {

    public class VaultInfoBase {

        [FromQuery(Name = "buc")]
        public string? BucketName { get; set; } //Sometimes we dont' want any root dir to be specified. We direclty start uploading.

        [FromQuery(Name = "rep")]
        public long? RepoId { get; set; } //Sometimes we dont' want any root dir to be specified. We direclty start uploading.

        public VaultInfoBase() {
        }
    }
}