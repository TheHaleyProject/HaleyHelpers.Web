using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Haley.Models {

    public class VaultFileInfo : VaultInfo { //YES.. Read base is based on write base.

        [FromQuery(Name = "tn")]
        public string? TargetName { get; set; }
        [FromQuery(Name = "pn")]
        public string? ProcessedName { get; set; }
        [FromQuery(Name = "uid")]
        public string? Cuid { get; set; }
        public VaultFileInfo() {
        }
    }
}