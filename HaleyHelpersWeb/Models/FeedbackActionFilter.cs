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

            do {
                if (ThrowTrace) break; //If we want to throw trace, we do nothing here.
                if (result is ObjectResult obj && obj.Value is IFeedbackBase feedback) { //If the result of the 
                    feedback.Trace = null;
                } else if (result is IFeedbackBase rawFeedback) {
                    rawFeedback.Trace = null; 
                    //resultContext.Result = new ObjectResult(rawFeedback); // Wrap it for serialization
                }
            } while (false); //Run once
        }
    }
}
