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

namespace Haley.Models {
    public class MultiPartUploadInput {
        public IStorageService StorageService { get; set; }
        public bool PreferId { get; set; } = true;
        public bool ParseIdFromKey { get; set; }
        public Func<string,string,long> IdGenerator { get; set; }
        public Func<KeyValueAccumulator,bool> DataHandler { get; set; }
        public int BufferSize { get; set; } = 8192; //8KB
        public MultiPartUploadInput() { }
    }
}
