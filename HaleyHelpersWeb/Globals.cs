﻿using Haley.Abstractions;
using Haley.Models;
using System.Security.Cryptography;
using System.Text;

namespace Haley.Utils {
    internal static class Globals {
        static JWTParameters _jwtParams;
        internal static IAdapterGateway DBService;
        internal static JWTParameters JWTParams => GetJWTParams();
        internal static JWTParameters GetJWTParams( bool force_reload = false) {
            try {
                if (_jwtParams == null || force_reload) {
                    _jwtParams = DBService?.GetConfigurationRoot(force_reload:force_reload)?.GetSection("Authentication")?.GetSection("Schemes")?.GetSection("JWT")?.Get<JWTParameters>();
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
