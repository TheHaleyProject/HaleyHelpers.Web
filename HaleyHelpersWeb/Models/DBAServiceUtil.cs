using Haley.Abstractions;
using Haley.Enums;
using Haley.Utils;

namespace Haley.Models {
    public class DBAServiceUtil : IGatewayUtil {
        public object Convert(object input) {
            if (input == null) return input;
            return WebHelperUtils.ConvertDBAResult(input);
        }
    }
}
