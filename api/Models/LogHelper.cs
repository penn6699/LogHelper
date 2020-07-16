/*
 * 日志助手
 * 具有调试信息记录、数据跟踪信息记录、错误信息记录、在线阅读日志等功能
 * by Penn.Lin 2020-07-15
 * 
 * 配置
 * Web.config 文件：
<appSettings>       
    <!-- 日志线程处理时间间隔。单位/毫秒，默认3000毫秒，最小值600毫秒。 -->
    <add key="LogHelper_LogThreadTimeOut" value="3000" />
    <!-- 日志等级：Off > Debug > Trace > Error -->
    <add key="LogHelper_LogLevel" value="Debug" />
    <!--日志数据库文件地址（绝对目录,格式：D:\\logs\\）【没有此配置或者值为空，取相对目录：App_Data\\logs\\ 】-->
    <add key="LogHelper_LogDbPathRoot" value="" /> 
</appSettings>
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using Newtonsoft.Json;

/// <summary>
/// 系统日志助手
/// </summary>
public static class LogHelper
{

    #region JsonHelper

    /// <summary>
    /// Json 助手
    /// </summary>
    public sealed class JsonHelper
    {
        /// <summary>
        /// 将对象序列化为JSON格式
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>json字符串</returns>
        public static string Serialize(object obj)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            string json = JsonConvert.SerializeObject(obj, settings);
            return json;
        }

        /// <summary>
        /// 解析JSON字符串生成对象实体
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="json">json字符串(例如：{"ID":"aa"})</param>
        /// <returns>对象实体</returns>
        public static T Deserialize<T>(string json) where T : class
        {
            JsonSerializer serializer = new JsonSerializer();
            StringReader sr = new StringReader(json);
            object o = serializer.Deserialize(new JsonTextReader(sr), typeof(T));
            T t = o as T;
            return t;
        }

        /// <summary>
        /// 解析JSON数组生成对象实体集合
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="json">json数组字符串(例如：[{"ID":"aaa"}])</param>
        /// <returns>对象实体集合</returns>
        public static List<T> DeserializeToList<T>(string json) where T : class
        {
            JsonSerializer serializer = new JsonSerializer();
            StringReader sr = new StringReader(json);
            object o = serializer.Deserialize(new JsonTextReader(sr), typeof(List<T>));
            List<T> list = o as List<T>;
            return list;
        }

        /// <summary>
        /// 反序列化JSON到给定的匿名对象.
        /// </summary>
        /// <typeparam name="T">匿名对象类型</typeparam>
        /// <param name="json">json字符串</param>
        /// <param name="anonymousTypeObject">匿名对象</param>
        /// <returns>匿名对象</returns>
        public static T DeserializeAnonymousType<T>(string json, T anonymousTypeObject)
        {
            T t = JsonConvert.DeserializeAnonymousType(json, anonymousTypeObject);
            return t;
        }

    }



    #endregion


    #region LogLevel

    /// <summary>
    /// 日志等级
    /// </summary>
    private static class LogLevel
    {
        /// <summary>
        /// 日志等级
        /// </summary>
        private static Dictionary<string, int> _Level = new Dictionary<string, int>() {
             { "error",0 }
            ,{ "trace", 1 }
            ,{ "debug", 2 }
            ,{ "off", 1000 }
        };

        /// <summary>
        /// 获取配置日志等级
        /// </summary>
        private static int ConfigLoggerLevel
        {
            get
            {
                try
                {
                    string lel = System.Configuration.ConfigurationManager.AppSettings["LogHelper_LogLevel"] ?? "Error";
                    return _Level[lel.ToLower()];
                }
                catch { 
                    return _Level["off"];
                }
            }
        }

        /// <summary>
        /// 是否可以写log
        /// </summary>
        /// <param name="Level"></param>
        /// <returns></returns>
        public static bool CanLog(string Level)
        {
            try
            {
                int lel = _Level[Level];
                return lel <= ConfigLoggerLevel;
            }
            catch
            {
                return false;
            }
        }


    }

    #endregion


    #region LogContent

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    private class LogContent
    {
        public string ID = null;
        /// <summary>
        /// 
        /// </summary>
        public DateTime Time;
        /// <summary>
        /// 
        /// </summary>
        public string Level = null;
        /// <summary>
        /// 类型
        /// </summary>
        public string Type = null;
        /// <summary>
        /// 消息
        /// </summary>
        public string Message = null;
        /// <summary>
        /// 数据
        /// </summary>
        public string Data = null;


        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserID = null;
        /// <summary>
        /// 用户名称
        /// </summary>
        public string UserName = null;
        /// <summary>
        /// 用户IP
        /// </summary>
        public string UserIP = null;


        /// <summary>
        /// 
        /// </summary>
        public LogContent() { Time = DateTime.Now; }

    }

    #endregion


    #region LogDB

    /// <summary>
    /// 
    /// </summary>
    private static class LogDbHelper
    {
        
        #region 日志写入数据库

        /// <summary>
        /// 是否初始化
        /// </summary>
        private static bool IsInit = false;
        /// <summary>
        /// 日志处理时间间隔。单位/毫秒，默认3000毫秒，最小值600毫秒。
        /// </summary>
        private static int ThreadTimeOut
        {
            get
            {
                try
                {
                    string TimeOutString = System.Configuration.ConfigurationManager.AppSettings["LogHelper_LogThreadTimeOut"] ?? "3000";
                    if (string.IsNullOrEmpty(TimeOutString))
                    {
                        TimeOutString = "3000";
                    }

                    int TimeOut = Convert.ToInt32(TimeOutString);
                    if (TimeOut < 600)
                    {
                        TimeOut = 600;
                    }
                    return TimeOut;
                }
                catch
                {
                    return 3000;
                }
            }
        }
        /// <summary>
        /// 日志文件根目录
        /// </summary>
        private static string LogDbPathRoot
        {
            get
            {
                string root = System.Configuration.ConfigurationManager.AppSettings["LogHelper_LogDbPathRoot"] ?? "";
                if (string.IsNullOrEmpty(root))
                {
                    root = HttpRuntime.AppDomainAppPath + "App_Data\\logs\\";
                }

                return root;
            }
        }
        

        // db name:log
        private static Dictionary<string, Dictionary<string, LogContent>> _LogPool = new Dictionary<string, Dictionary<string, LogContent>>();


        /// <summary>
        /// 写日志到队列
        /// </summary>
        /// <param name="logInfo"></param>
        public static void Write(string DbName, LogContent content)
        {
            //线程初始化
            if (!IsInit)
            {
                Thread thread = new Thread(WriterWork);
                thread.Name = "LogHelper_LogDbHelper_Thread";
                thread.IsBackground = true;
                thread.Start();

                IsInit = true;
            }

            if (!_LogPool.ContainsKey(DbName))
            {
                _LogPool[DbName] = new Dictionary<string, LogContent>();
            }
            _LogPool[DbName].Add(Guid.NewGuid().ToString("N"), content);

        }

        /// <summary>
        /// 文件锁
        /// </summary>
        private static object locker = new object();
        /// <summary>
        /// 日志写入数据库
        /// </summary>
        private static void WriterWork()
        {
            while (true)
            {
                lock (locker)
                {
                    try
                    {
                        if (_LogPool.Count > 0)
                        {
                            KeyValuePair<string, Dictionary<string, LogContent>> dbDIC = _LogPool.FirstOrDefault();
                            if (dbDIC.Value.Count < 1)
                            {
                                _LogPool.Remove(dbDIC.Key);
                                Thread.Sleep(ThreadTimeOut);
                            }
                            else
                            {

                                List<SQLiteParams> parList = new List<SQLiteParams>();
                                List<string> keys = new List<string>();

                                foreach (KeyValuePair<string, LogContent> kvp in dbDIC.Value)
                                {
                                    keys.Add(kvp.Key);

                                    SQLiteParams par = new SQLiteParams();

                                    #region sql

                                    par.Sql = @"
INSERT INTO Logs (Time,Level,Type,Message,Data,UserID,UserName,UserIP)
VALUES(@Time,@Level,@Type,@Message,@Data,@UserID,@UserName,@UserIP)
";
                                    
                                    #endregion

                                    par.Parameters.Add(new SQLiteParameter("@Time", kvp.Value.Time));
                                    par.Parameters.Add(new SQLiteParameter("@Type", string.IsNullOrEmpty(kvp.Value.Type) ? (object)DBNull.Value : kvp.Value.Type));
                                    par.Parameters.Add(new SQLiteParameter("@Level", string.IsNullOrEmpty(kvp.Value.Level) ? (object)DBNull.Value : kvp.Value.Level));
                                    par.Parameters.Add(new SQLiteParameter("@Message", string.IsNullOrEmpty(kvp.Value.Message) ? (object)DBNull.Value : kvp.Value.Message));
                                    par.Parameters.Add(new SQLiteParameter("@Data", string.IsNullOrEmpty(kvp.Value.Data) ? (object)DBNull.Value : kvp.Value.Data));
                                    par.Parameters.Add(new SQLiteParameter("@UserID", string.IsNullOrEmpty(kvp.Value.UserID) ? (object)DBNull.Value : kvp.Value.UserID));
                                    par.Parameters.Add(new SQLiteParameter("@UserName", string.IsNullOrEmpty(kvp.Value.UserName) ? (object)DBNull.Value : kvp.Value.UserName));
                                    par.Parameters.Add(new SQLiteParameter("@UserIP", string.IsNullOrEmpty(kvp.Value.UserIP) ? (object)DBNull.Value : kvp.Value.UserIP));

                                    parList.Add(par);
                                }

                                if (parList.Count > 0)
                                {
                                    WriteToDB(dbDIC.Key, parList);

                                    foreach (string key in keys)
                                    {
                                        if (dbDIC.Value.ContainsKey(key))
                                        {
                                            dbDIC.Value.Remove(key);
                                        }

                                    }


                                }

                            }

                        }

                    }
                    catch { }
                    //每 ThreadTimeOut 毫秒处理一次
                    Thread.Sleep(ThreadTimeOut);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private class SQLiteParams
        {

            /// <summary>
            /// SQL语句
            /// </summary>
            public string Sql;
            /// <summary>
            /// 参数集合
            /// </summary>
            public List<SQLiteParameter> Parameters;

            /// <summary>
            /// 构造函数
            /// </summary>
            public SQLiteParams()
            {
                Sql = string.Empty;
                Parameters = new List<SQLiteParameter>();
            }
        }

        /// <summary>
        /// 日志写入数据库
        /// </summary>
        /// <param name="DbName">20200110_debug</param>
        /// <param name="pars"></param>
        private static void WriteToDB(string DbName, List<SQLiteParams> pars)
        {
            string year = DbName.Substring(0, 4);
            string month = DbName.Substring(4, 2);
            string DbFolderPath = LogDbPathRoot + "\\" + year + "\\" + month;
            if (!Directory.Exists(DbFolderPath))
            {
                Directory.CreateDirectory(DbFolderPath);
            }
            string DbPath = DbFolderPath + "\\" + DbName + ".db";
            bool IsExistDb = File.Exists(DbPath);
            SQLiteConnection conn = new SQLiteConnection("Data Source=" + DbPath + ";Pooling=true;Max Pool Size=100;");
            conn.Open();
            if (!IsExistDb)
            {
                string sqlDB = @"
CREATE TABLE IF NOT EXISTS Logs (
    ID         INTEGER       PRIMARY KEY AUTOINCREMENT
                             NOT NULL,
    Time       DATETIME      NOT NULL
                             DEFAULT (datetime('now', 'localtime') ),
    Level      VARCHAR (150)  NOT NULL,
    Type       VARCHAR (150)  NOT NULL,
    UserID     VARCHAR (50),
    UserName   VARCHAR (150),
    UserIP     VARCHAR (50),
    Message    TEXT,
    Data       TEXT
);
";
                SQLiteCommand cmdDB = new SQLiteCommand(sqlDB, conn);
                cmdDB.ExecuteNonQuery();
                cmdDB.Dispose();
            }

            SQLiteTransaction transaction = conn.BeginTransaction();
            try
            {

                foreach (SQLiteParams par in pars)
                {
                    SQLiteCommand cmd = new SQLiteCommand(par.Sql, conn);
                    cmd.Transaction = transaction;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 60;//单位为秒
                    if (par.Parameters != null && par.Parameters.Count > 0)
                    {
                        cmd.Parameters.AddRange(par.Parameters.ToArray());
                    }
                    cmd.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch (Exception exp)
            {
                transaction.Rollback();
                throw exp;
            }
            finally
            {
                transaction.Dispose();
            }

        }



        #endregion


        #region 读取日志

        /// <summary>
        /// 获取日志列表
        /// </summary>
        /// <returns></returns>
        public static List<Dictionary<string, object>> GetLogDbFileList(string yearStr = "", string monthStr = "")
        {

            List<Dictionary<string, object>> res = new List<Dictionary<string, object>>();
            string logPath = LogDbPathRoot;

            if (!Directory.Exists(logPath))
            {
                return res;
            }

            DirectoryInfo root = new DirectoryInfo(logPath);
            foreach (DirectoryInfo year in root.GetDirectories())
            {
                if (!string.IsNullOrEmpty(yearStr) && yearStr != year.Name) { continue; }
                foreach (DirectoryInfo month in year.GetDirectories())
                {
                    if (!string.IsNullOrEmpty(monthStr) && monthStr != month.Name) { continue; }

                    foreach (FileInfo logFile in month.GetFiles())
                    {
                        Dictionary<string, object> log = new Dictionary<string, object>();
                        log.Add("year", year.Name);
                        log.Add("month", month.Name);
                        log.Add("fileName", logFile.Name);
                        log.Add("filePath", string.Format(@"{0}\{1}\{2}", year.Name, month.Name, logFile.Name));
                        log.Add("fileUpdateTime", logFile.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                        log.Add("fileSize", logFile.Length);//字节

                        res.Add(log);
                    }
                }
            }

            return res;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DbFilePath">数据库相对URL</param>
        /// <returns></returns>
        public static DataTable GetLogs(string DbFilePath)
        {

            string DbPath = LogDbPathRoot + "\\" + DbFilePath;
            SQLiteConnection conn = new SQLiteConnection("Data Source=" + DbPath + ";Pooling=true;Max Pool Size=100;");
            conn.Open();
            string sql = @"SELECT ID,Time,Level,Type,UserID,UserName,UserIP,Message FROM Logs";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                System.Data.Common.DbTransaction transaction = cmd.Connection.BeginTransaction();
                try
                {
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                    DataTable data = new DataTable();
                    adapter.Fill(data);

                    transaction.Commit();

                    adapter.Dispose();

                    return data;
                }
                catch (Exception exp)
                {
                    transaction.Rollback();
                    throw exp;
                }
                finally
                {
                    transaction.Dispose();
                    cmd.Connection.Close();
                }

            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DbFilePath">数据库相对URL</param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static DataTable GetLogData(string DbFilePath, string ID)
        {

            string DbPath = LogDbPathRoot + "\\" + DbFilePath;
            SQLiteConnection conn = new SQLiteConnection("Data Source=" + DbPath + ";Pooling=true;Max Pool Size=100;");
            conn.Open();
            string sql = @"SELECT ID,Time,Level,Type,UserID,UserName,UserIP,Message,Data FROM Logs where ID=@ID";

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                System.Data.Common.DbTransaction transaction = cmd.Connection.BeginTransaction();
                try
                {
                    cmd.Parameters.Add(new SQLiteParameter("@ID", ID));
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd);
                    DataTable data = new DataTable();
                    adapter.Fill(data);
                    transaction.Commit();

                    adapter.Dispose();


                    return data;
                }
                catch (Exception exp)
                {
                    transaction.Rollback();
                    throw exp;
                }
                finally
                {
                    transaction.Dispose();
                    cmd.Connection.Close();
                }

            }

        }

        #endregion

    }

    #endregion


    #region Logger Write

    /// <summary>
    /// 格式化
    /// </summary>
    /// <param name="LogLevel">级别</param>
    /// <param name="Message">消息</param>
    /// <param name="Data">数据</param>
    /// <param name="LogType">类型</param>
    /// <param name="IsUnUserAuthentication">不经过用户验证</param>
    /// <returns></returns>
    private static LogContent CreateLogContent(string LogLevel, string Message, object Data = null, string LogType = "系统", bool IsUnUserAuthentication = false)
    {
        LogContent log = new LogContent();
        log.Time = DateTime.Now;

        log.Level = LogLevel;
        log.Message = Message;

        #region Data

        if (Data == null || Data is DBNull)
        {
            log.Data = "";
        }
        /*
        else if (Data is Exception) {
            Exception exp = (Exception) Data;
            string expData = LogJson.Serialize(exp.Data);
            _data = LogJson.Serialize(new Dictionary<string, string>() {
                { "ExceptionMessage",exp.Message},
                { "ExceptionData",expData},
                { "ExceptionStackTrace",exp.StackTrace}
            });
        }
        */
        else if (
            Data is sbyte || Data is short || Data is int || Data is long || Data is byte || Data is ushort ||
            Data is uint || Data is ulong || Data is decimal ||
            Data is bool ||
            Data is char || Data is string || Data is System.Text.StringBuilder
        )
        {
            log.Data = Data.ToString();
        }
        else
        {
            log.Data = JsonHelper.Serialize(Data);
        }

        #endregion

        log.Type = string.IsNullOrEmpty(LogType) ? "系统" : LogType;

        if (log.Type == "Logger" || log.Type == "系统")
        {
            log.UserID = "logger";
            log.UserName = "系统日志助手";
        }


        if (HttpContext.Current != null)
        {
            HttpRequest Request = HttpContext.Current.Request;

            if (Request.ServerVariables["HTTP_VIA"] != null)
            {
                log.UserIP = Convert.ToString(Request.ServerVariables["HTTP_X_FORWARDED_FOR"]);
            }
            else
            {
                log.UserIP = Convert.ToString(Request.ServerVariables["REMOTE_ADDR"]);
            }

            //if (!IsUnUserAuthentication)
            //{
            //    try
            //    {
            //        LoginUserInfo lui = null;
            //        if (SessionHelper.IsOnline())
            //        {
            //            lui = SessionHelper.CurrentUser;
            //        }
            //        if (lui != null)
            //        {
            //            log.UserID = Convert.ToString(lui.user_id);
            //            log.UserName = lui.user_realname;
            //        }
            //        else
            //        {
            //            log.UserID = "logger";
            //            log.UserName = "系统日志助手";
            //        }
            //    }
            //    catch
            //    {
            //        log.UserID = "logger";
            //        log.UserName = "系统日志助手";
            //    }
            //}

        }


        return log;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Message"></param>
    /// <param name="Data"></param>
    /// <param name="LogType"></param>
    /// <param name="IsUnUserAuthentication">是否不经过用户验证</param>
    public static void Error(string Message, object Data = null, string LogType = "系统", bool IsUnUserAuthentication = false) {
        try
        {
            if (LogLevel.CanLog("error"))
            {
                LogContent content = CreateLogContent("error", Message, Data, LogType, IsUnUserAuthentication);
                LogDbHelper.Write(content.Time.ToString("yyyyMMdd") + "_error", content);
            }
        }
        catch { }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Message"></param>
    /// <param name="Data"></param>
    /// <param name="LogType"></param>
    /// <param name="IsUnUserAuthentication">是否不经过用户验证</param>
    public static void Trace(string Message, object Data = null, string LogType = "系统", bool IsUnUserAuthentication = false)
    {
        try
        {
            if (LogLevel.CanLog("trace"))
            {
                LogContent content = CreateLogContent("trace", Message, Data, LogType, IsUnUserAuthentication);
                LogDbHelper.Write(content.Time.ToString("yyyyMMdd") + "_trace", content);
            }
        }
        catch { }
    }

    /// <summary>
    /// 调试记录
    /// </summary>
    /// <param name="Message"></param>
    /// <param name="Data"></param>
    /// <param name="LogType"></param>
    /// <param name="IsUnUserAuthentication">是否不经过用户验证</param>
    public static void Debug(string Message, object Data = null, string LogType = "系统", bool IsUnUserAuthentication = false)
    {
        try
        {
            if (LogLevel.CanLog("debug"))
            {
                LogContent content = CreateLogContent("debug", Message, Data, LogType, IsUnUserAuthentication);
                LogDbHelper.Write(content.Time.ToString("yyyyMMdd") + "_debug", content);
            }
        }
        catch { }
    }

    #endregion


    #region Logger Read

    /// <summary>
    /// 获取日志数据库列表
    /// </summary>
    /// <param name="yearStr">4位年份字符串</param>
    /// <param name="monthStr">2位月份字符串</param>
    /// <returns></returns>
    public static List<Dictionary<string, object>> GetLogDbFileList(string yearStr = "", string monthStr = "") {
        return LogDbHelper.GetLogDbFileList(yearStr, monthStr);
    }
    /// <summary>
    /// 获取日志列表
    /// </summary>
    /// <param name="DbFilePath">数据库相对URL</param>
    /// <returns></returns>
    public static DataTable GetLogs(string DbFilePath) {
        return LogDbHelper.GetLogs(DbFilePath);
    }
    /// <summary>
    /// 获取日志数据
    /// </summary>
    /// <param name="DbFilePath">数据库相对URL</param>
    /// <param name="ID">日志ID</param>
    /// <returns></returns>
    public static DataTable GetLogData(string DbFilePath, string ID) {
        return LogDbHelper.GetLogData(DbFilePath, ID);
    }

    #endregion


}