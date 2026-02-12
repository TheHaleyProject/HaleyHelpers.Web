using Haley.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Haley.Utils {
    public static class PolicyMakerExtensions {
        private const string CLAIM_OID = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string CLAIM_TID = "http://schemas.microsoft.com/identity/claims/tenantid";
        private static string[] ROLE_MAP = new string[] {"role", "roles", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" };
        public static void CreateAuthPolicy(this AuthorizationOptions options, string policy_name, string scheme_name, Action<AuthorizationPolicyBuilder>? processor = null) { 
            options.CreateAuthPolicy(policy_name, new string[] { scheme_name }, processor: processor);
        }
        static void CreateAuthPolicy(this AuthorizationOptions options, string policy_name, string[] scheme_names, string[] requiredRoles =null, string[] requiredClaims = null, IAuthorizationRequirement[] customRequirements = null, Action<AuthorizationPolicyBuilder>? processor = null, bool allRoles = false, bool allClaims = false, bool enforceScheme = false) {

            if (string.IsNullOrWhiteSpace(policy_name)) throw new ArgumentNullException("Policy name cannot be null or empty");
            //An authorization policy doesn't  need a scheme.. It can authorize from any other sceheme (using the authentication middle ware). We can only force the policy. So, if scheme_names is null or empty, we will not set the scheme in the policy and it will work with any authenticated scheme. But, if scheme_names are provided, then we will set the scheme in the policy and it will work only with the provided schemes.

            var schemes = scheme_names?.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToArray();

            //WE JUST SPECIFY THAT WE NEED AN AUTHENTICATED USER, THATS IT.. SCHEME DOESN'T MATTER.
            var policyBuilder = schemes?.Length > 0
                ? new AuthorizationPolicyBuilder(schemes).RequireAuthenticatedUser()
                : new AuthorizationPolicyBuilder().RequireAuthenticatedUser();

            //ROLES VALIDATION
            if (requiredRoles != null && requiredRoles.Length > 0) {
                var roles = requiredRoles.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
               
                policyBuilder.RequireAssertion(ctx => {
                    var userRoles = ctx.User.Claims
                        .Where(c => ROLE_MAP.Contains(c.Type,StringComparer.OrdinalIgnoreCase))
                        .Select(c => c.Value)
                        .ToList();
                    //    policyBuilder.RequireRole(roles.ToArray()); //Here, role is OR condition. Presence of any role will be accepted.
                    return allRoles
                        ? roles.All(r => userRoles.Contains(r, StringComparer.OrdinalIgnoreCase))
                        : roles.Any(r => userRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
                });
            }

            //CLAIMS VALIDATION
            if (requiredClaims != null && requiredClaims.Length > 0) {
                var claims = requiredClaims.Where(p => !string.IsNullOrWhiteSpace(p)).Select(q=> q.MapKnownType()).Distinct().ToList();
                policyBuilder.RequireAssertion(ctx=> {
                    var userClaims = ctx.User.Claims.Select(c => c.Type.MapKnownType()).ToList();
                    return allClaims
                        ? claims.All(rc => userClaims.Any(uc => uc.Equals(rc, StringComparison.OrdinalIgnoreCase)))
                        : claims.Any(rc => userClaims.Any(uc => uc.Equals(rc, StringComparison.OrdinalIgnoreCase)));
                    //policyBuilder.RequireClaim(claim); //issues with MapKnownTypes
                });
            }

            // CUSTOM REQUIREMENTS HANDLER
            if (customRequirements != null && customRequirements.Length > 0) {
                foreach (var requirement in customRequirements) {
                    policyBuilder.AddRequirements(requirement);
                }
            }

            //ENFORCE SCHEME
            if (enforceScheme) policyBuilder.AddRequirements(new EnforcePolicySchemeRequirement()); 

            processor?.Invoke(policyBuilder);
            options.AddPolicy(policy_name, policyBuilder.Build());
        }

        static string MapKnownType(this string claimName) {
            return claimName.ToLowerInvariant() switch {
                ClaimTypes.Email or "email" => ClaimTypes.Email,
                ClaimTypes.Name or "name" => ClaimTypes.Name,
                ClaimTypes.GivenName or "given_name" or "givenname" => ClaimTypes.GivenName,
                ClaimTypes.Surname or "family_name" or "surname" => ClaimTypes.Surname,
                ClaimTypes.NameIdentifier or "sub" or "nameid" => ClaimTypes.NameIdentifier,
                ClaimTypes.Role or "role" or "roles" => ClaimTypes.Role,
                ClaimTypes.Upn or "upn" => ClaimTypes.Upn,
                CLAIM_OID or "oid" => "oid",
                CLAIM_TID or "tid" => "tid",
                ClaimTypes.GroupSid or "groups" => ClaimTypes.GroupSid,
                _ => claimName
            };
        }

        public static AuthPolicyMaker WithSchemes(this AuthorizationOptions options, params string[] scheme_names) => new AuthPolicyMaker() { SchemeNames = scheme_names, Options = options };
        public static AuthPolicyMaker WithRequirements(this AuthorizationOptions options, params IAuthorizationRequirement[] requirements) => new AuthPolicyMaker() { CustomRequirements = requirements, Options = options };
        public static AuthPolicyMaker RequireRoles(this AuthorizationOptions options, params string[] role_names) => new AuthPolicyMaker() { RoleNames = role_names, Options = options };
        public static AuthPolicyMaker RequireClaims(this AuthorizationOptions options, params string[] claim_names) => new AuthPolicyMaker() { ClaimNames = claim_names, Options = options };
        public static AuthPolicyMaker RequireAllRoles(this AuthorizationOptions options, params string[] role_names) => new AuthPolicyMaker() { RoleNames = role_names, Options = options , AllRoles = true};
        public static AuthPolicyMaker RequireAllClaims(this AuthorizationOptions options, params string[] claim_names) => new AuthPolicyMaker() { ClaimNames = claim_names, Options = options, AllClaims = true };
        public static AuthPolicyMaker ForPolicy(this AuthorizationOptions options, string policy_name) => new AuthPolicyMaker() { PolicyName = policy_name, Options = options };
        public static AuthPolicyMaker EnforceScheme(this AuthorizationOptions options) => new AuthPolicyMaker() { Options = options , EnforceScheme = true};

        public static AuthPolicyMaker WithSchemes(this AuthPolicyMaker input, params string[] scheme_names) {
            input.SchemeNames = scheme_names;
            return input;
        }
        public static AuthPolicyMaker WithRequirements(this AuthPolicyMaker input, params IAuthorizationRequirement[] requirements) {
            input.CustomRequirements = requirements;
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
        public static AuthPolicyMaker EnforceScheme(this AuthPolicyMaker input) {
            input.EnforceScheme = true;
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
                customRequirements: input.CustomRequirements,
                requiredClaims: input.ClaimNames, 
                processor: processor,
                enforceScheme: input.EnforceScheme, //default is false
                allRoles:input.AllRoles,
                allClaims:input.AllClaims);
        }
    }
}
