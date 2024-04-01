using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Haley.Models;

namespace ThreeBIM.Controllers {

    public class DBAController : ControllerBase {
        private DBAdapterDictionary _adapterDic;

        public DBAController(DBAdapterDictionary adapter_dic) {
            _adapterDic = adapter_dic;
        }

        [AllowAnonymous]
        [Route("Refresh")]
        [HttpGet]
        public async Task RefreshConfigs() {
            _adapterDic.UpdateAdapter(); //this will generate a new Configuration root for all the already existing data and then update the connections accordingly.
        }

        [AllowAnonymous]
        [Route("GetEntries")]
        [HttpGet]
        public async Task<object> GetDBAEntries() {
            var result = DBAdapterDictionary.Instance.Values.Select(p => new {
                Type = p.Entry.DBType.ToString(),
                DB = p.Entry.DBName,
                Schema = p.Entry.SchemaName,
                Key = p.Entry.AdapterKey
            });
            return result;
        }
    }
}