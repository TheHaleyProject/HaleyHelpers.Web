using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Haley.Models {

    public class VaultRead : VaultInfoBase { //YES.. Read base is based on write base.

        [Required]
        [FromQuery(Name = "tn")]
        public string TargetName { get; set; }

        public VaultRead() {
        }
    }
}