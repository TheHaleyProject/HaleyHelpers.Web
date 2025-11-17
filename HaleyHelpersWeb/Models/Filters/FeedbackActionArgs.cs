using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haley.Models {
    public class FeedbackActionArgs {
        public bool DisplayTraceMessage { get; set; }
        public Func<IActionResult?, Task> Handler { get; set; }
    }
}
