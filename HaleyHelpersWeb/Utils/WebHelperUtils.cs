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

        public static string GetClientIP(this HttpRequest request) {
            try {
                string result = string.Empty;
                var host = request.Host; //Request Host
                if (host.HasValue) {
                    if (host.Host == "localhost") return host.Host;
                    IPAddress ipAddress = IPAddress.Parse(host.Host);
                    IPHostEntry ipHostEntry = Dns.GetHostEntry(ipAddress);
                }

                return result;
            } catch (Exception) {
                return null;
            }
        }
    }
}
