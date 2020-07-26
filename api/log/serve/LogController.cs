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
        /// <param name="bDate"></param>
        /// <param name="eDate"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        [HttpGet]
        public dynamic SearchLogDatabase(string bDate = "", string eDate = "", string level = "")
        {
            List<LogHelper.LogDbFile> logFiles = new List<LogHelper.LogDbFile>();


            string yearStr = "";
            string monthStr = "";
            if (!string.IsNullOrEmpty(bDate) && bDate == eDate) {
                DateTime bTime = DateTime.Parse(bDate);
                yearStr = bTime.ToString("yyyy");
                monthStr = bTime.ToString("MM");
            }

            if (string.IsNullOrEmpty(level))
            {
                logFiles = LogHelper.GetLogDbFileList(yearStr, monthStr, "");
            }
            else {
                string[] levels = level.Split(new char[] { ',' });
                foreach (string _level in levels) {
                    if (!string.IsNullOrEmpty(_level))
                    {
                        logFiles.AddRange(LogHelper.GetLogDbFileList(yearStr, monthStr, _level));
                    }
                }
            }


            if ( string.IsNullOrEmpty(bDate) && string.IsNullOrEmpty(eDate) )
            {
                return logFiles;
            }

            if (!string.IsNullOrEmpty(bDate))
            {
                DateTime bTime = DateTime.Parse(bDate);
                logFiles = logFiles.FindAll(row => row.logTime >= bTime);
            }

            if (!string.IsNullOrEmpty(eDate))
            {
                DateTime eTime = DateTime.Parse(eDate);
                logFiles = logFiles.FindAll(row => row.logTime <= eTime);
            }

            return logFiles;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet,HttpPost]
        public dynamic GetLogs()
        {

            System.Web.HttpRequest Request = System.Web.HttpContext.Current.Request;
            string path = Request["path"] ?? "";
            string bTime = Request["bTime"] ?? "";
            string eTime = Request["eTime"] ?? "";
            
            string Type = Request["Type"] ?? "";
            string UserID = Request["UserID"] ?? "";
            string UserName = Request["UserName"] ?? "";
            string UserIP = Request["UserIP"] ?? "";
            string Message = Request["Message"] ?? "";
            string Data = Request["Data"] ?? "";

            if (string.IsNullOrEmpty(path))
            {
                return new List<Dictionary<string, object>>();
            }
            if (path.Contains(',')) {
                List<Dictionary<string, object>> logs = new List<Dictionary<string, object>>();

                foreach (string _path in path.Split(new char[] { ',' })) {
                    if (!string.IsNullOrEmpty(_path))
                    {
                        logs.AddRange(LogHelper.GetLogs2(_path, bTime, eTime, Type, UserID, UserName, UserIP, Message, Data));
                    }
                }
                return logs;
            }
            else
            {
                return LogHelper.GetLogs2(path, bTime, eTime, Type, UserID, UserName, UserIP, Message, Data);
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="id"></param>
        /// <returns></returns>
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
