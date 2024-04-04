using Haley.Abstractions;
using Haley.Enums;
using Haley.Utils;

namespace Haley.Models {
    public class DBAServiceUtil : IDBServiceUtil {
        public Task<object> GetFirst(object input, ResultFilter filter) {
            if (input == null) return null;
            return Task.FromResult(input.GetFirst(filter));
        }
    }
}
