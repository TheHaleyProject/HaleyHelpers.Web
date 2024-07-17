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
using Haley.Enums;

namespace Haley.Models {
    public class VaultRequestWrite : VaultRequestBase {
        [FromQuery(Name = "src")]
        public bool SourceFromKey { get; set; } = true; //Or Source from value / FileName
        [FromQuery(Name ="fh")]
        public bool ForceHash { get; set; } = false; //If not do not parse. For hash if parse fails, generate hash.
        [FromQuery(Name = "prefNum")]
        public bool PreferNumeric { get; set; } = true; //Or else go for hash
        public VaultRequestWrite() { }
    }
}
