using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Xml;
using Haley.Models;

namespace Haley.Utils {
    public class SamlHelpers {
        // Common SAML/Entra URIs
        internal const string NsSaml = "urn:oasis:names:tc:SAML:2.0:assertion";
        internal const string pathNameId = "//saml:Assertion/saml:Subject/saml:NameID";
        internal const string pathAttribute = "//saml:AttributeStatement/saml:Attribute";

        // Azure AD (Microsoft Entra) typical claim URIs
        private const string CLAIM_EMAIL = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
        private const string CLAIM_NAME = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
        private const string CLAIM_UPN = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";
        private const string CLAIM_OID = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string CLAIM_TID = "http://schemas.microsoft.com/identity/claims/tenantid";
        private const string CLAIM_GRP = "http://schemas.microsoft.com/ws/2008/06/identity/claims/groups";

        /// <summary>
        /// Extracts a baseline set of claims from a validated SAML Response/Assertion.
        /// </summary>
        public static List<Claim> ExtractClaims(XmlDocument doc, string issuer) {
            var claims = new List<Claim>();
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("saml", NsSaml);

            // NameID
            var nameId = doc.SelectSingleNode(pathNameId, ns)?.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(nameId))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, nameId, ClaimValueTypes.String, issuer));

            // Attribute statements
            foreach (XmlElement attr in doc.SelectNodes(pathAttribute, ns)!) {
                var name = attr.GetAttribute("Name")?.Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                foreach (XmlElement valNode in attr.GetElementsByTagName("AttributeValue", NsSaml)) {
                    var value = valNode.InnerText?.Trim();
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    var mappedType = MapKnownType(name);
                    claims.Add(new Claim(mappedType, value, ClaimValueTypes.String, issuer));
                }
            }

            // Optional: ensure a friendly display name fallback
            if (!claims.Any(c => c.Type == ClaimTypes.Name)) {
                var upn = claims.FirstOrDefault(c => c.Type == "upn")?.Value
                          ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                          ?? nameId;
                if (!string.IsNullOrWhiteSpace(upn))
                    claims.Add(new Claim(ClaimTypes.Name, upn, ClaimValueTypes.String, issuer));
            }

            return claims;
        }
        public static string BuildAuthnRedirectUrl(string ssoUrl, SamlAuthOptions opts, string relayState = "/") {
            // Create SAML 2.0 AuthnRequest XML
            var id = "_" + Guid.NewGuid().ToString("N");
            var issueInstant = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var xml = $@"
                <samlp:AuthnRequest
                    xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol""
                    xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion""
                    ID=""{id}""
                    Version=""2.0""
                    IssueInstant=""{issueInstant}""
                    ProtocolBinding=""urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST""
                    AssertionConsumerServiceURL=""{opts.AcsUrl}"">
                    <saml:Issuer>{opts.SpEntityId}</saml:Issuer>
                    <samlp:NameIDPolicy Format=""urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified"" AllowCreate=""true"" />
                </samlp:AuthnRequest>";

            // Compress and Base64-encode the AuthnRequest
            var bytes = Encoding.UTF8.GetBytes(xml);
            using var output = new MemoryStream();
            using (var deflate = new DeflateStream(output, CompressionMode.Compress, true)) {
                deflate.Write(bytes, 0, bytes.Length);
                deflate.Close();
            }
                
            var deflated = output.ToArray();
            var samlRequest = Convert.ToBase64String(deflated);
            // Build the redirect URL with query params
            var query = $"SAMLRequest={Uri.EscapeDataString(samlRequest)}&RelayState={Uri.EscapeDataString(relayState)}";
            return $"{ssoUrl}?{query}";
        }
        private static string MapKnownType(string samlName) {
            return samlName switch {
                CLAIM_EMAIL => ClaimTypes.Email,
                CLAIM_NAME => ClaimTypes.Name,
                CLAIM_UPN => "upn",
                CLAIM_OID => "oid",
                CLAIM_TID => "tid",
                CLAIM_GRP => ClaimTypes.GroupSid, // or a custom "groups" type if you prefer
                _ => samlName // keep original URI for unknowns
            };
        }
    }
}
