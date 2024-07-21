using Microsoft.AspNetCore.Mvc;
using Haley.Abstractions;
using Microsoft.AspNetCore.WebUtilities;
using Haley.Utils;

namespace Haley.Models {
    public class RepoWriteWrapper : VaultRequestWrapper {
        public string RepoName { get; set; }
        public RepoWriteWrapper(IStorageManager storage, VaultWrite input):base(storage,input) {
            
        }
    }
}
