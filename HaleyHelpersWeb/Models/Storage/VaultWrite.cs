using Haley.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Haley.Models {

    public class VaultWrite : VaultInfoBase {

        [FromQuery(Name = "res")]
        public ObjectExistsResolveMode ResolveMode { get; set; } = ObjectExistsResolveMode.ReturnError;

        public VaultWrite() {
        }
    }
}