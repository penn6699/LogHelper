using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

namespace api.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [EnableCors("*","*","*",SupportsCredentials =true)]
    public class LogController : ApiController
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="yearStr"></param>
        /// <param name="monthStr"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        [HttpGet]
        public dynamic SearchLogDbFiles(string yearStr = "", string monthStr = "", string level = "") {
            return LogHelper.GetLogDbFileList(yearStr, monthStr, level);
        }

        [HttpGet]
        public dynamic GetLogs(string path)
        {
            return LogHelper.GetLogs(path);
        }

        [HttpGet]
        public dynamic GetLogData(string path,string id)
        {
            return LogHelper.GetLogData(path,id);
        }



        [HttpGet]
        public dynamic test()
        {

            LogHelper.Error("Error" + "_" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), new {
                a= Guid.NewGuid().ToString("N")
            });
            System.Threading.Thread.Sleep(15);
            LogHelper.Debug("Debug" + "_" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), new
            {
                b = Guid.NewGuid().ToString("N")
            });
            System.Threading.Thread.Sleep(15);
            LogHelper.Trace("Trace" + "_" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), new
            {
                c = Guid.NewGuid().ToString("N")
            });

            System.Threading.Thread.Sleep(10);




            return Guid.NewGuid().ToString("N") + "_" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }














    }
}
