﻿using Microsoft.AspNetCore.Mvc;
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
    public class VaultRequestRead : VaultRequestBase {
        [Required]
        [FromQuery(Name = "sn")]
        public string StoredName { get; set; }
        [FromQuery(Name ="fn")]
        public string FileNameToSave { get; set; }
        public VaultRequestRead() { }
    }
}