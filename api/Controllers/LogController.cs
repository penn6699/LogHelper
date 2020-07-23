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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="yearStr"></param>
        /// <param name="monthStr"></param>
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

        [HttpGet]
        public dynamic GetLogs(string path
             , string bTime = "", string eTime = "", string Level = "", string Type = "", string UserID = ""
            , string UserName = "", string UserIP = "", string Message = "", string Data = ""
            )
        {
            return LogHelper.GetLogs(path, bTime, eTime, Level, Type, UserID, UserName, UserIP, Message, Data);
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
