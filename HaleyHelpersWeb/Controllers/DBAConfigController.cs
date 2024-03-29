using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Haley.Models;

namespace ThreeBIM.Controllers {

    public class DBAConfigController : ControllerBase {
        private DBAdapterDictionary _adapterDic;

        public DBAConfigController(DBAdapterDictionary adapter_dic) {
            _adapterDic = adapter_dic;
        }

        [AllowAnonymous]
        [Route("Refresh")]
        [HttpGet]
        public async Task RefreshConfigs() {
            _adapterDic.UpdateAdapter(); //this will generate a new Configuration root for all the already existing data and then update the connections accordingly.
        }
    }
}