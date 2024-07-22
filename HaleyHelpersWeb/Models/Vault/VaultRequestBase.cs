using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Buffers;
using System.Collections.ObjectModel;
using Haley.Abstractions;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;

namespace Haley.Models {
    public class VaultRequestBase {
        [FromQuery(Name ="vg")]
        public string? RootDir { get; set; } //Sometimes we dont' want any root dir to be specified. We direclty start uploading.
        [Required]
        [FromQuery(Name = "key")]
        public string ServiceKey { get; set; }
        public VaultRequestBase() { }
    }
}
