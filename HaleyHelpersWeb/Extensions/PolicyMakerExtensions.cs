using Microsoft.AspNetCore.Authorization;
using Haley.Models;

namespace Haley.Utils {
    public static class PolicyMakerExtensions {
        public static void CreateAuthPolicy(this AuthorizationOptions options, string policy_name, string scheme_name, Action<AuthorizationPolicyBuilder>? processor = null) { 
            options.CreateAuthPolicy(policy_name, new string[] { scheme_name }, processor: processor);
        }
        public static void CreateAuthPolicy(this AuthorizationOptions options, string policy_name, string[] scheme_names, string[] requiredRoles =null, string[] requiredClaims = null, Action<AuthorizationPolicyBuilder>? processor = null, bool allRoles = false, bool allClaims = false) {

            if (string.IsNullOrWhiteSpace(policy_name)) throw new ArgumentNullException("Policy name cannot be null or empty");
            if (scheme_names == null || scheme_names.Count() < 1) throw new ArgumentNullException("Scheme name cannot be null or empty");

            var schemes = scheme_names.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToArray();
            if (schemes.Length < 1) throw new ArgumentNullException("Atleast one valid Scheme name is required");

            var policyBuilder = new AuthorizationPolicyBuilder(schemes) //Api Key
                            .RequireAuthenticatedUser();

            if (requiredRoles != null && requiredRoles.Length > 0) {
                var roles = requiredRoles.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();

                if (!allRoles) {
                    policyBuilder.RequireRole(roles.ToArray()); //Here, role is OR condition. Presence of any role will be accepted.
                } else {
                    policyBuilder.RequireAssertion(ctx =>
                     roles.All(r => ctx.User.IsInRole(r))
                    );
                }
            }

            if (requiredClaims != null && requiredClaims.Length > 0) {
                var claims = requiredClaims.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
                if (!allClaims) {
                    foreach (var claim in claims) {
                        policyBuilder.RequireClaim(claim);
                    }
                } else {
                    policyBuilder.RequireAssertion(ctx =>
                     //claims.All(c => ctx.User.HasClaim(c, ctx.User.FindFirst(c)?.Value))
                     claims.All(rc => ctx.User.Claims.Any(c => c.Type == rc))  
                    );
                }
            }

            processor?.Invoke(policyBuilder);
            options.AddPolicy(policy_name, policyBuilder.Build());
        }
        public static AuthPolicyMaker WithSchemes(this AuthorizationOptions options, params string[] scheme_names) => new AuthPolicyMaker() { SchemeNames = scheme_names, Options = options };
        public static AuthPolicyMaker RequireRoles(this AuthorizationOptions options, params string[] role_names) => new AuthPolicyMaker() { RoleNames = role_names, Options = options };
        public static AuthPolicyMaker RequireClaims(this AuthorizationOptions options, params string[] claim_names) => new AuthPolicyMaker() { ClaimNames = claim_names, Options = options };

        public static AuthPolicyMaker RequireAllRoles(this AuthorizationOptions options, params string[] role_names) => new AuthPolicyMaker() { RoleNames = role_names, Options = options , AllRoles = true};
        public static AuthPolicyMaker RequireAllClaims(this AuthorizationOptions options, params string[] claim_names) => new AuthPolicyMaker() { ClaimNames = claim_names, Options = options, AllClaims = true };

        public static AuthPolicyMaker ForPolicy(this AuthorizationOptions options, string policy_name) => new AuthPolicyMaker() { PolicyName = policy_name, Options = options };

        public static AuthPolicyMaker WithSchemes(this AuthPolicyMaker input, params string[] scheme_names) {
            input.SchemeNames = scheme_names;
            return input;
        }
        public static AuthPolicyMaker RequireRoles(this AuthPolicyMaker input, params string[] role_names) {
            input.RoleNames = role_names;
            return input;
        }
        public static AuthPolicyMaker RequireClaims(this AuthPolicyMaker input, params string[] claim_names) {
            input.ClaimNames = claim_names;
            return input;
        }

        public static AuthPolicyMaker RequireAllRoles(this AuthPolicyMaker input, params string[] role_names) {
            input.RoleNames = role_names;
            input.AllRoles = true;
            return input;
        }
        public static AuthPolicyMaker RequireAllClaims(this AuthPolicyMaker input, params string[] claim_names) {
            input.ClaimNames = claim_names;
            input.AllClaims = true;
            return input;
        }
        public static AuthPolicyMaker ForPolicy(this AuthPolicyMaker input, string policy_name) {
            input.PolicyName = policy_name;
            return input;
        }

        public static void CreateAuthPolicy(this AuthPolicyMaker input, string policyname = null, string schemename=null, Action<AuthorizationPolicyBuilder>? processor = null) {
            if (input.Options == null) throw new ArgumentNullException("AuthorizationOptions cannot be null");
            if (!string.IsNullOrWhiteSpace(policyname)) input.PolicyName = policyname;
            if (!string.IsNullOrWhiteSpace(schemename)) {
                if (input.SchemeNames == null || input.SchemeNames.Length == 0) {
                    input.SchemeNames = new string[] { schemename };
                } else {
                    var list = input.SchemeNames.ToList();
                    list.Add(schemename);
                    input.SchemeNames = list.Distinct().ToArray();
                }
            }
            input.Options.CreateAuthPolicy(
                policy_name: input.PolicyName, 
                scheme_names: input.SchemeNames,
                requiredRoles: input.RoleNames, 
                requiredClaims: input.ClaimNames, 
                processor: processor,
                allRoles:input.AllRoles,
                allClaims:input.AllClaims);
        }
    }
}
