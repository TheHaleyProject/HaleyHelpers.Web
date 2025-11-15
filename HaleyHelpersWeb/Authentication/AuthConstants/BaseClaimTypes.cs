namespace Haley.Models {
    public class BaseClaimTypes {
        public const string SCHEMA_NAME = nameof(SCHEMA_NAME); //"schname";
        public const string DB_NAME = nameof(DB_NAME); //"dbname";
        public const string DBA_KEY = nameof(DBA_KEY); //"dbakey";
        public const string RESOURCE_ID = nameof(RESOURCE_ID);
        public const string COMPANY_ID = nameof(COMPANY_ID);
        public const string COMPANY_GUID = nameof(COMPANY_GUID);

        public const string IS_DEVELOPER = nameof(IS_DEVELOPER);
        public const string IS_SUPER_ADMIN = nameof(IS_SUPER_ADMIN);
        public const string IS_ADMIN = nameof(IS_ADMIN);

        public const string CAN_EDIT = nameof(CAN_EDIT);
        public const string CAN_DELETE = nameof(CAN_DELETE);
        public const string CAN_CREATE = nameof(CAN_CREATE);
    }
}
