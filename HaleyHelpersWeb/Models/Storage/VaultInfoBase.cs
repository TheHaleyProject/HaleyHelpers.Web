using Microsoft.AspNetCore.Mvc;

namespace Haley.Models {

    public class VaultInfoBase {

        [FromQuery(Name = "cn")]
        public string? ContainerName { get; set; } //Sometimes we dont' want any root dir to be specified. We direclty start uploading.

        [FromQuery(Name = "ucid")]
        public long? UserContainerId { get; set; } //Sometimes we dont' want any root dir to be specified. We direclty start uploading.

        public VaultInfoBase() {
        }
    }
}