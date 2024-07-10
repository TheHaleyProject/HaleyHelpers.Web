using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Haley.Models {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class MultipartFormInputAttribute : Attribute, IResourceFilter {
        public void OnResourceExecuted(ResourceExecutedContext context) {
            
        }

        public void OnResourceExecuting(ResourceExecutingContext context) {
            var request = context.HttpContext.Request;
          
            //If we receive multipart/form-data, return without throwing any kind of error.
            if (request != null &&
                request.HasFormContentType
                && request.ContentType!.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase)) {
                return;
            }
            context.Result = new StatusCodeResult(StatusCodes.Status415UnsupportedMediaType);
        }
    }
}
