namespace Haley.Models {
    public class BaseClaimTypes {
        public const string SCHEMA_NAME = nameof(SCHEMA_NAME); //"schname";
        public const string DB_NAME = nameof(DB_NAME); //"dbname";
        public const string DBA_KEY = nameof(DBA_KEY); //"dbakey";
        public const string RELAY_STATE = nameof(RELAY_STATE); //"dbakey";

        //Base Ids
        public const string RESOURCE_ID = nameof(RESOURCE_ID);
        public const string COMPANY_ID = nameof(COMPANY_ID);
        public const string COMPANY_GUID = nameof(COMPANY_GUID);
        public const string PROJECT_ID = nameof(PROJECT_ID);
        public const string SUBSCRIPTION_ID = nameof(SUBSCRIPTION_ID);
        public const string TENANT_ID = nameof(TENANT_ID);
        public const string SESSION_ID = nameof(SESSION_ID);

        //Base 
        public const string CAN_EDIT = nameof(CAN_EDIT);
        public const string CAN_DELETE = nameof(CAN_DELETE);
        public const string CAN_CREATE = nameof(CAN_CREATE);
        public const string CAN_UPDATE = nameof(CAN_UPDATE);
        public const string CAN_APPROVE = nameof(CAN_APPROVE);
    }
}
