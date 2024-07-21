using Microsoft.AspNetCore.Mvc;
using Haley.Abstractions;
using Microsoft.AspNetCore.WebUtilities;
using Haley.Utils;

namespace Haley.Models {
    public class VaultRequestWrapper  {
        public VaultRequestBase Request { get; set; }
        public IStorageService Service { get; set; }
        public Func<(string key, string value, VaultRequestBase req),string> FileNameGenerator { get; set; }
        public Func<KeyValueAccumulator,bool> DataHandler { get; set; }
        public bool RepoMode { get; set; } = false;
        public int BufferSize { get; set; } = 8192; //8KB
        public VaultRequestWrapper(IStorageManager storage, VaultRequestBase input) {
            if (storage == null || input == null || input.ServiceKey == null) throw new ArgumentNullException($@"{nameof(IStorageManager)}, {nameof(VaultWrite)} and Service Key cannot be null");
            Request = input;
            Service = storage[Request.ServiceKey];
            //FileNameGenerator = DefaultFileNameGenerator;
        }

        private string DefaultFileNameGenerator((string key, string value, VaultRequestBase req) input) {
            //this will be the default generator.
            //Key is Id , Value is supposedly the Raw File Name.
            StorageRequest sreq = new StorageRequest();
            input.req?.MapProperties(sreq);
            sreq.Id = input.key;
            sreq.RawName = input.value;
            //Assuming it is file
            sreq.GenerateTargetName();
            return sreq.TargetName;
        }
    }
}
