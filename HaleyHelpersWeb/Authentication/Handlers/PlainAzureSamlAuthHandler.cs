using Haley.Abstractions;
using Haley.Enums;
using Haley.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text.Encodings.Web;
using System.Xml;

namespace Haley.Models {
    public sealed class PlainAzureSamlAuthHandler : PlainAuthHandlerBase<SamlAuthOptions> {
        public PlainAzureSamlAuthHandler(IOptionsMonitor<SamlAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override PlainAuthMode AuthMode { get; set; } = PlainAuthMode.AzureSAML;

        protected override Func<HttpContext, string, ILogger, Task<PlainAuthResult>>? PrepareClaims => async (ctx, base64Xml, log) => SamlHelpers.ValidateAzurePayload(Request,base64Xml,log,Scheme.Name, OptionsMonitor.Get(Scheme.Name));
    }
}
