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
        public FileNameSource Source { get; set; } = FileNameSource.Id; //Or Source from value / FileName
        [FromQuery(Name ="fh")]
        public bool ForcedHash { get; set; } = false; //If not do not parse. For hash if parse fails, generate hash.
        [FromQuery(Name = "pref")]
        public FileNamePreference Preference { get; set; } = FileNamePreference.Number; //Or else go for hash
        public VaultRequestWrite() { }
    }
}
