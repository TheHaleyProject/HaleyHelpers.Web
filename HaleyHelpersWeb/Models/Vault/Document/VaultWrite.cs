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
    public class VaultWrite : VaultRequestBase {
        [FromQuery(Name = "src")]
        public StorageNameSource Source { get; set; } = StorageNameSource.Id; //Or Source from value / FileName
        [FromQuery(Name ="hm")]
        public StorageNameHashMode HashMode { get; set; } = StorageNameHashMode.ParseOrCreate;
        [FromQuery(Name = "pref")]
        public StorageNamePreference Preference { get; set; } = StorageNamePreference.Number; //Or else go for hash
        [FromQuery(Name = "rm")]
        public StorageFileConflict ResolveMode { get; set; } = StorageFileConflict.ReturnError;
        public VaultWrite() { }
    }
}
