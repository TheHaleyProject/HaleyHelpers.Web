using System.Text;

namespace Haley.Models {
    public class JWTParameters {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public bool ValidateIssuer { get; set; }
        public bool ValidateAudience { get; set; }
        public byte[] GetSecret() {
            return Encoding.ASCII.GetBytes(Secret);
            //var _secret = Encoding.UTF8.GetString(Convert.FromBase64String(Secret));
            //return Encoding.ASCII.GetBytes(_secret);
        }
        public JWTParameters() { }
    }
}
