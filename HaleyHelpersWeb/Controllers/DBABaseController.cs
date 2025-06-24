using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Haley.Abstractions;
using Haley.Utils;

namespace Haley.Models {

    public abstract class DBABaseController : ControllerBase {
        private IAdapterGateway _dbservice;

        public DBABaseController(IAdapterGateway dbservice) {
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

            return _dbservice.Values.Select(p => new {
                Type = p.Info.DBType.ToString(),
                DB = p.Info.DBName,
                Schema = p.Info.SchemaName,
                Key = p.Info.AdapterKey,
                Host = AdapterGateway.ParseConnectionString(p.Info.ConnectionString,"host=")
            });
        }
    }
}