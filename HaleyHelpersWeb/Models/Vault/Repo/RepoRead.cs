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
    public class RepoRead : RepoBase {
        //[FromQuery(Name = "src")]
        //public StorageNameSource Source { get; set; } = StorageNameSource.Id; //Or Source from value / FileName
        [FromQuery(Name = "tn")]
        public string? TargetName { get; set; }
        [FromQuery(Name ="path")]
        public string? Path { get; set; }   
        public RepoRead() { }
    }
}
