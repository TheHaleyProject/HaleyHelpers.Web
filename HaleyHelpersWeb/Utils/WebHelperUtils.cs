using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Net;
using System.Text;
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

        public static string GetEncryptedIPCookie(this HttpContext context, string encryptKey, string encryptSalt = null, Dictionary<string, string> payload = null) {
            var ip = context.GetClientIP();
            if (string.IsNullOrWhiteSpace(ip)) throw new ArgumentException("Unable to verify the ip address of the request.");
            if (string.IsNullOrWhiteSpace(encryptKey)) throw new ArgumentException("Encryption Key cannot be empty or null");
            if (string.IsNullOrWhiteSpace(encryptSalt)) encryptSalt = encryptKey; //assign the key as the salt as well

            var cookieDic = new Dictionary<string, string> {
                ["ip"] = ip
            };
            if (payload != null) {
                foreach (var item in payload) {
                    if (!cookieDic.ContainsKey(item.Key)) {
                        cookieDic.Add(item.Key, item.Value);
                    }
                }
            }

            return EncryptionUtils.Encrypt(cookieDic.ToJson(), encryptKey, encryptSalt).value;
        }

        public static void AppendEncryptIPCookie(this HttpContext context, string cookieName, CookieOptions options, string encryptKey, string encryptSalt = null, Dictionary<string,string> payload = null) {
            try {
                if (context == null) throw new ArgumentNullException("HttpContext");
                if (string.IsNullOrWhiteSpace(cookieName)) throw new ArgumentNullException("CookieName is required for creation");
                var cookie = GetEncryptedIPCookie(context, encryptKey, encryptSalt, payload);
                context.Response.Cookies.Append(cookieName, cookie, options);
            } catch (Exception) {
                context.Response.Body = new MemoryStream(Encoding.UTF8.GetBytes("Error while trying to add cookie"));
            }
        }

        public static Dictionary<string,string> DecryptIPCookie (this string cookie, string encryptKey, string encryptSalt = null) {
            if (string.IsNullOrWhiteSpace(cookie)) throw new ArgumentNullException("Cookie value cannot be null for decryption.");
            if (string.IsNullOrWhiteSpace(encryptKey)) throw new ArgumentException("Encryption Key cannot be empty or null");
            if (string.IsNullOrWhiteSpace(encryptSalt)) encryptSalt = encryptKey; //assign the key as the salt as well
            var decrypted = EncryptionUtils.Decrypt(cookie,encryptKey, encryptSalt);
            return decrypted.FromJson<Dictionary<string, string>>();
        }
    }
}
