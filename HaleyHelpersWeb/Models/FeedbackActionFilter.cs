using Haley.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Haley.Models {
    public class FeedbackActionFilter : IAsyncActionFilter {
        bool ThrowTrace = true;
        public FeedbackActionFilter(bool throwTrace= true) {
            ThrowTrace = throwTrace;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
            var resultContext = await next();

            var result = resultContext.Result;

            if (result is ObjectResult obj && obj.Value is IFeedback feedback) { //If the result of the 
                if (!_env.IsDevelopment()) {
                    feedback.HideDebugInfo(); // Or sanitize, wrap, etc.
                }
            } else if (result is IFeedback rawFeedback) {
                if (!_env.IsDevelopment()) {
                    rawFeedback.HideDebugInfo();
                }

                resultContext.Result = new ObjectResult(rawFeedback); // Wrap it for serialization
            }
        }
    }
}
