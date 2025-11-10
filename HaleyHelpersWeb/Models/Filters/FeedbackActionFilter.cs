using Haley.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection.Metadata.Ecma335;

namespace Haley.Models {
    public class FeedbackActionFilter : IAsyncActionFilter {
        FeedbackActionArgs _args;
        Func<IActionResult?, Task> _handler;
        public FeedbackActionFilter(FeedbackActionArgs args) { 
            _args = args;
            if (_args.Handler != null) {
                _handler = args.Handler;
            }else {
                _handler = HandleResults;
            }
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
            var resultContext = await next();
            var result = resultContext?.Result;
            await _handler.Invoke(result);
        }

        async Task HandleResults(IActionResult? result) {
            if (result == null) return;
            if (_args.ThrowExceptions) return; //If we want to throw trace, we do nothing here.
            if (result is ObjectResult obj && obj.Value is IFeedbackBase feedback) { //If the result of the 
                feedback.Trace = null;
            } else if (result is IFeedbackBase rawFeedback) {
                rawFeedback.Trace = null;
                //resultContext.Result = new ObjectResult(rawFeedback); // Wrap it for serialization
            }
        }
    }
}
