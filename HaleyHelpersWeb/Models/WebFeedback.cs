using Haley.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Haley.Models {
    public class WebFeedback : Feedback, IWebFeedback {
        public HttpStatusCode StatusCode { get; set; }
        public WebFeedback SetStatusCode(HttpStatusCode code) {
            StatusCode = code;
            return this;
        }
        public WebFeedback() {  }
        public WebFeedback(HttpStatusCode code, string message, object result) : base(message, result) { StatusCode = code; }
        public WebFeedback(HttpStatusCode code):this(code,null,null)  { }
        public WebFeedback(HttpStatusCode code, string message) : this(code,message,null) { }
    }
}
