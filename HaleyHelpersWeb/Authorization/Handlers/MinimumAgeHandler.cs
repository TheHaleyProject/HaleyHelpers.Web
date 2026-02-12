using Haley.Abstractions;
using Haley.Enums;
using Haley.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Haley.Models {
    internal class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement> {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumAgeRequirement requirement) {
            // Get the date of birth claim
            var dobClaim = context.User.FindFirst(c => c.Type == ClaimTypes.DateOfBirth);

            if (dobClaim == null) {
                return Task.CompletedTask;  // Fail - no DOB claim
            }

            if (!DateTime.TryParse(dobClaim.Value, out var dob)) {
                return Task.CompletedTask;  // Fail - invalid date
            }

            var age = DateTime.Today.Year - dob.Year;
            if (dob.Date > DateTime.Today.AddYears(-age)) age--;

            if (age >= requirement.MinimumAge) {
                context.Succeed(requirement);  // User is old enough
            }

            return Task.CompletedTask;
        }
    }
}
