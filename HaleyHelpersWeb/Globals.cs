using Haley.Models;
using System.Security.Cryptography;
using System.Text;

namespace Haley {
    internal static class Globals {
        static JWTParameters _jwtParams;
        internal static JWTParameters JWTParams => GetJWTParams();
        internal static JWTParameters GetJWTParams(bool reload = false) {
            try {
                if (_jwtParams == null || reload) {
                    _jwtParams = DBAdapterDictionary.Instance.GetConfigurationRoot(reload)?.GetSection("JWTParameters")?.Get<JWTParameters>();
                }
                return _jwtParams;
            } catch (Exception) {
                return null;
            }
        }

        internal static void HandleConfigUpdate() {
            //we need to reload the JWT parameters.
            GetJWTParams(true);
        }
    }
}
