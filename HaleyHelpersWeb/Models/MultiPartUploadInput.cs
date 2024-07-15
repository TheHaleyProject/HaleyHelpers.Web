using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Buffers;
using System.Collections.ObjectModel;
using Haley.Abstractions;

namespace Haley.Models {
    public class MultiPartUploadInput {
        public IStorageService StorageService { get; set; }
        public bool PreferId { get; set; } = true;
        public bool ParseNameAsId { get; set; } = true;
        public bool FallBackToName { get; set; } = false;
        public int BufferSize { get; set; } = 8192; //8KB
        public MultiPartUploadInput() { }
    }
}
