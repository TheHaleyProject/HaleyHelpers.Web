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
    internal class EnforcePolicySchemeHandler : AuthorizationHandler<EnforcePolicySchemeRequirement> {
        private readonly IAuthorizationPolicyProvider _policyProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EnforcePolicySchemeHandler(IAuthorizationPolicyProvider policyProvider, IHttpContextAccessor httpContextAccessor) {
            _policyProvider = policyProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EnforcePolicySchemeRequirement requirement) {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) {
                return;
            }

            // Get the policy being evaluated
            var endpoint = httpContext.GetEndpoint();
            var authorizeData = endpoint?.Metadata.GetMetadata<IAuthorizeData>(); //Policy associated with the the end point.

            if (authorizeData?.Policy == null) return;

            // Get the actual policy object
            var policy = await _policyProvider.GetPolicyAsync(authorizeData.Policy);
            if (policy == null || !policy.AuthenticationSchemes.Any()) {
                // No schemes specified in policy - allow any authenticated user
                context.Succeed(requirement);
                return;
            }

            // Check if user was authenticated via one of the allowed schemes
            var authenticationType = context.User.Identity?.AuthenticationType;

            if (!string.IsNullOrEmpty(authenticationType) &&
                policy.AuthenticationSchemes.Contains(authenticationType, StringComparer.OrdinalIgnoreCase)) {
                context.Succeed(requirement);
            } else {
                // User authenticated via a scheme not allowed by this policy
                context.Fail();
            }
        }
    }
}
