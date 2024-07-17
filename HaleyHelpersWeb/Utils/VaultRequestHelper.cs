using Microsoft.AspNetCore.Mvc;
using Haley.Abstractions;
using Microsoft.AspNetCore.WebUtilities;
using Haley.Models;

namespace Haley.Utils {
    public class VaultRequestHelper {
        public IStorageService Service { get; set; }
        public Func<(string key, string value,VaultRequestWrite req),object> FileNameGenerator { get; set; }
        public Func<KeyValueAccumulator,bool> DataHandler { get; set; }
        public int BufferSize { get; set; } = 8192; //8KB
        public VaultRequestHelper(IStorageManager storage, VaultRequestWrite input) {
            if (storage == null || input == null || input.ServiceKey == null) throw new ArgumentNullException($@"{nameof(IStorageManager)}, {nameof(VaultRequestWrite)} and Service Key cannot be null");
            Service = storage[input.ServiceKey];
            FileNameGenerator = DefaultFileNameGenerator;
        }

        private object DefaultFileNameGenerator((string key, string value, VaultRequestWrite req) input) {
            //this will be the default generator.
            var req = input.req;
            if (req.SourceFromKey) {
                if (req.PreferNumeric) {
                    if (long.TryParse(input.key, out var keyId)) return keyId;
                    throw new ArgumentException($@"{input.key} is not a valid numeric value");
                } else {
                    if (req.ForceHash) {
                        //Regardless of whether the input is already in MD5 hash or not, we perform one more hash.
                        HashUtils.co
                    }
                    if (input.key.IsMD5()) return Pkcs9Lo
                }

            } else {

            }

            if (req.PreferNumeric && req.SourceFromKey) {
                //Use Key for 
            }
            if (req.PreferNumeric) {
                if (req.SourceFromKey) {
                    //consider key to generate the Name.
                } else {

                }
                return 0;
            } else {
                return "hello";
            }
        }
    }
}
