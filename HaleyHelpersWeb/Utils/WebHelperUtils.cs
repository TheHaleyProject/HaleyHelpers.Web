using Haley.Enums;
using Haley.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Web;

namespace Haley.Utils {
    public static class WebHelperUtils {
        public static object ConvertDBAResult(this object input, ResultFilter filter = ResultFilter.FirstDictionaryValue) {
            //If we send error
            if (input is DBAError dbaerr) {
                return new BadRequestObjectResult(dbaerr.ToString());
            }
            if (input is DBAResult dbres) {
                return new OkObjectResult(dbres.ToString());
            }
            //If we send direct action result
            if (typeof(IActionResult).IsAssignableFrom(input.GetType())) return input;
            return input;
        }

        public static string GetClientIP(this HttpContext context) {
            try {
                return context.Connection.RemoteIpAddress?.ToString();
            } catch (Exception) {
                return string.Empty;
            }
        }

        public static string GenerateIPInfoCookieValue(this HttpContext context, string salt) {
            var ip = context.GetClientIP();
            if (string.IsNullOrWhiteSpace(ip)) throw new ArgumentException("Unable to verify the ip address of the request.");
            var cookieval = GenerateEncryptedCookieValue(ip,salt);
            return cookieval;
        }

        public static string GenerateEncryptedCookieValue(string payload, string salt, string key = null) {
            if (string.IsNullOrWhiteSpace(key)) {
                key = DateTime.UtcNow.ToShortDateString(); //Cookie encrypted with todays date.
            }
            if (string.IsNullOrWhiteSpace(salt)) throw new ArgumentException("Please provide salt for encrypting the cookie");

            var cookieval = EncryptionUtils.Symmetric.Encrypt(payload, key, salt).value.ComputeHash(HashMethod.Sha256, false);
            return cookieval;
        }
    }
}
