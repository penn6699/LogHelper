using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace api.Controllers
{
    public class TestController : ApiController
    {
        [HttpGet]
        public dynamic t1() {
            LogHelper.Debug("Debug", new { 
                a=455
            });
            LogHelper.Error("Error", new
            {
                a = 123
            });
            LogHelper.Trace("Error", new
            {
                a = 789
            });
            return DateTime.Now.Ticks;
        }





    }
}
