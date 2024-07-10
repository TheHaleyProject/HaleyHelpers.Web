using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Haley.Models {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false )]
    public class DisableFormBindingAttribute : Attribute, IResourceFilter {
        public void OnResourceExecuted(ResourceExecutedContext context) {
            
        }

        public void OnResourceExecuting(ResourceExecutingContext context) {
            //By default, when a request is made, .Net will inspect the body and if a multi-part form data is found it will fetch it into memory or into disk.
            //This defies our whole purpose. We should not allow default processing. It also has certain limits of upto 64K temporary files creation.
            //We go ahead and disable default factories.
            var factories = context.ValueProviderFactories;
            factories.RemoveType<FormValueProviderFactory>();
            factories.RemoveType<FormFileValueProviderFactory>();
            factories.RemoveType<JQueryFormValueProviderFactory>();
        }
    }
}
