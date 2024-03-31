using Haley.Models;
using Microsoft.AspNetCore.Mvc;

namespace Haley.Utils {
    public static class DBAExecuter {
        static DBAdapterDictionary _dba;
        public static DBAdapterDictionary DBA => GetDBA();

        static DBAdapterDictionary GetDBA() {
            if (_dba == null) { _dba = DBAdapterDictionary.Instance; }
            return _dba;
        }

        public static void SetDBA(DBAdapterDictionary dba) { _dba = dba; }

        //This should be stateless as every controller might call this concurrently.
        public static async Task<object> Read(HttpContext context,  ILogger logger, string query,params (string key,object value)[] parameters) {
           return await ExecuteInternal(true, context, logger, query,parameters);
        }

        public static async Task<object> NonQuery(HttpContext context, ILogger logger, string query, params (string key, object value)[] parameters) {
            return await ExecuteInternal(false, context, logger, query,parameters);
        }

        static async Task<object> ExecuteInternal(bool isread,HttpContext context, ILogger logger, string query, params (string key, object value)[] parameters) {
            string dbakey = string.Empty;
            try {
                if (context == null) return new NotFoundObjectResult("HttpContext is null.");
                if (query == null) return new NotFoundObjectResult("Query is null.");
                var jwt = await context.ParseToken(logger);
                if (jwt == null) return new NotFoundObjectResult("JWT token is null. Exception while trying to fetch the claims from the JWT token. Ensure the JWTBearerOptions has SaveToken set to true.");
                dbakey = jwt?.Claims?.FirstOrDefault(p => p.Type == JWTClaimType.DBA_KEY)?.Value;
                object result = null;
                switch (isread) {
                    case true:
                    result =  (await DBA[dbakey]?.ExecuteReader(query, null,parameters))?.Select(true)?.Convert();
                    break;
                    case false:
                    default:
                    result = await DBA[dbakey]?.ExecuteNonQuery(query, null, parameters);
                    break;
                }
                return result;
            } catch (Exception ex) {
                logger.LogError($@"Error while trying to execute read operation for key {dbakey} with query - {query} . {Environment.NewLine} {ex.Message}");
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
