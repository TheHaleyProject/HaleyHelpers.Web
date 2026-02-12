namespace Haley {
    //REMEMBER: AUTHENTICATION IS DIFFERENT FROM AUTHORIZATION.
    //1. Scheme also tells us where the credentials come from (like headers, cookies, tokens), while Policy tells us the conditions that must be met (like roles, claims, permissions) to define what authenticated users are authorized to do (like access certain resources or perform specific actions). Example: Admin policy, Viewer Policy, General Policy etc.
    //2. Scheme is about validating credentials, Policy is about enforcing rules.
    //3. Scheme is configured in the authentication middleware, Policy is configured in the authorization middleware.
    //4. Scheme basically tells us "who you are" while Policy tells us "what you can do".
    public class BasePolicyNames {
        public const string Any = "Plain-Policy-Any";

        public const string General = "Plain-Policy-General";
        public const string Reader = "Plain-Policy-Read-Only";
        public const string Creator = "Plain-Policy-Create";
        public const string Admin = "Plain-Policy-Admin";
        public const string SuperAdmin = "Plain-Policy-Super-Admin";
        public const string Developer = "Plain-Policy-Developer";

        public const string DocumentAccess = "Plain-Doc-Access";
        public const string AuthExchange = "Plain-User-Auth-Exchange";
    }
}
