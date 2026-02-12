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
    internal class MustUseSchemeHandler : AuthorizationHandler<MustUseSchemeRequirement> {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MustUseSchemeRequirement requirement) {
            // Check the actual authentication type used
            var authenticationType = context.User.Identity?.AuthenticationType;

            if (authenticationType != null && requirement.AllowedSchemes.Contains(authenticationType,StringComparer.OrdinalIgnoreCase)) {
                context.Succeed(requirement);
            } else {
                // Explicitly fail if wrong scheme
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
