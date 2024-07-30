using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Haley.Abstractions;

namespace Haley.Models {

    public class DBABase : ControllerBase {
        private IDBService _dbservice;

        public DBABase(IDBService dbservice) {
            _dbservice = dbservice;
        }

        [AllowAnonymous]
        [Route("Refresh")]
        [HttpGet]
        public async Task RefreshConfigs() {
            _dbservice?.UpdateAdapter(); //this will generate a new Configuration root for all the already existing data and then update the connections accordingly.
        }

        [AllowAnonymous]
        [Route("ForceReload")]
        [HttpGet]
        public async Task ReloadConfiguration() {
            _dbservice?.GetConfigurationRoot(true,true); //this will generate a new Configuration root for all the already existing data and then update the connections accordingly.
        }

        [AllowAnonymous]
        [Route("GetEntries")]
        [HttpGet]
        public async Task<object> GetDBAEntries() {
            return DBAService.Instance.Values.Select(p => new {
                Type = p.Entry.DBType.ToString(),
                DB = p.Entry.DBName,
                Schema = p.Entry.SchemaName,
                Key = p.Entry.AdapterKey,
                Host = DBAService.ParseConnectionString(p.Entry.ConnectionString,"host=")
            });
        }
    }
}