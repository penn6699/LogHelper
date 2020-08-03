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
public sealed class LogHelper
{

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


    #region 内部类

    /// <summary>
    /// 日志内容
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
    /// <summary>
    /// 日志数据文件信息
    /// </summary>
    [Serializable]
    public class LogDbFile
    {
        /// <summary>
        /// 4位年份
        /// </summary>
        public string year = null;
        /// <summary>
        /// 2位月份
        /// </summary>
        public string month = null;
        public string date = null;
        public DateTime logTime;
        public string level = null;
        /// <summary>
        /// 文件名
        /// </summary>
        public string fileName = null;
        /// <summary>
        /// 文件相对路径
        /// </summary>
        public string filePath = null;
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? fileUpdateTime = null;
        /// <summary>
        /// 文件大小。单位/字节
        /// </summary>
        public long fileSize = 0;
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
        /// <param name="yearStr"></param>
        /// <param name="monthStr"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static List<LogDbFile> GetLogDbFileList(string yearStr = "", string monthStr = "", string level = "")
        {

            List<LogDbFile> res = new List<LogDbFile>();
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
                        
                        if (!string.IsNullOrEmpty(level) && !logFile.Name.Contains(level)) { continue; }
                        try
                        {
                            LogDbFile logDb = new LogDbFile();
                            logDb.year = year.Name;
                            logDb.month = month.Name;
                            logDb.fileName = logFile.Name;
                            logDb.filePath = string.Format(@"{0}\{1}\{2}", year.Name, month.Name, logFile.Name);
                            logDb.fileUpdateTime = logFile.LastWriteTime;
                            logDb.fileSize = logFile.Length;

                            logDb.date = logFile.Name.Substring(0, 8);
                            logDb.level = Path.GetFileNameWithoutExtension(logFile.Name).Replace(logDb.date + "_", "");
                            logDb.logTime = DateTime.Parse(logDb.year + "-" + logDb.month + "-" + logFile.Name.Substring(6, 2) + " 00:00:00");

                            res.Add(logDb);
                        }
                        catch(Exception exp) {
                            LogHelper.Error("读取日志列表异常。文件“" + logFile.Name +"”不符合日志数据命名格式。"+ exp.Message, exp);
                        }
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
        public static DataTable GetLogs(string DbFilePath
            ,string bTime="",string eTime="", string Type = "", string UserID = ""
            , string UserName = "", string UserIP = "", string Message = "", string Data = ""

            )
        {

            string DbPath = LogDbPathRoot + "\\" + DbFilePath;
            SQLiteConnection conn = new SQLiteConnection("Data Source=" + DbPath + ";Pooling=true;Max Pool Size=100;");
            conn.Open();
            string sql = @"SELECT ID,Time,Level,Type,UserID,UserName,UserIP,Message FROM Logs";

            List<string> whereList = new List<string>();
            List<SQLiteParameter> parList = new List<SQLiteParameter>();

            #region where par

            if (!string.IsNullOrEmpty(bTime)) {
                whereList.Add(@" Time >= @bTime ");
                parList.Add(new SQLiteParameter("@bTime", bTime));
            }
            if (!string.IsNullOrEmpty(eTime))
            {
                whereList.Add(@" Time <= @eTime ");
                parList.Add(new SQLiteParameter("@eTime", eTime));
            }
            
            if (!string.IsNullOrEmpty(Type))
            {
                whereList.Add(@" Type like '%'||@Type||'%' ");
                parList.Add(new SQLiteParameter("@Type", Type));
            }
            if (!string.IsNullOrEmpty(UserID))
            {
                whereList.Add(@" UserID=@UserID ");
                parList.Add(new SQLiteParameter("@UserID", UserID));
            }
            if (!string.IsNullOrEmpty(UserName))
            {
                whereList.Add(@" UserName like '%'||@UserName||'%' ");
                parList.Add(new SQLiteParameter("@UserName", UserName));
            }

            if (!string.IsNullOrEmpty(UserIP))
            {
                whereList.Add(@" UserIP like '%'||@UserIP||'%' ");
                parList.Add(new SQLiteParameter("@UserIP", UserIP));
            }
            if (!string.IsNullOrEmpty(Message))
            {
                whereList.Add(@" Message like '%'||@Message||'%' ");
                parList.Add(new SQLiteParameter("@Message", Message));
            }
            if (!string.IsNullOrEmpty(Data))
            {
                whereList.Add(@" Data like '%'||@Data||'%' ");
                parList.Add(new SQLiteParameter("@Data", Data));
            }


            #endregion


            if (whereList.Count>0) {
                sql += @" where " + string.Join(" and ", whereList);
            }

            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                System.Data.Common.DbTransaction transaction = cmd.Connection.BeginTransaction();
                try
                {
                    if (parList.Count > 0)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddRange(parList.ToArray());
                    }

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
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            log.Data = JsonConvert.SerializeObject(Data, settings);
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
    /// <param name="level">日志等级</param>
    /// <returns></returns>
    public static List<LogDbFile> GetLogDbFileList(string yearStr = "", string monthStr = "", string level = "") {
        return LogDbHelper.GetLogDbFileList(yearStr, monthStr, level);
    }
    /// <summary>
    /// 获取日志列表
    /// </summary>
    /// <param name="DbFilePath">数据库相对URL</param>
    /// <param name="bTime">开始时间</param>
    /// <param name="eTime">结束时间</param>
    /// <param name="Level">等级</param>
    /// <param name="Type">类型</param>
    /// <param name="UserID">用户ID</param>
    /// <param name="UserName">用户名</param>
    /// <param name="UserIP">访问IP</param>
    /// <param name="Message">消息</param>
    /// <param name="Data">数据</param>
    /// <returns></returns>
    public static DataTable GetLogs(string DbFilePath
            , string bTime = "", string eTime = "", string Type = "", string UserID = ""
            , string UserName = "", string UserIP = "", string Message = "", string Data = ""
        )
    {
        return LogDbHelper.GetLogs(DbFilePath, bTime, eTime, Type, UserID, UserName, UserIP, Message, Data);
    }
    /// <summary>
    /// 获取日志列表
    /// </summary>
    /// <param name="DbFilePath">数据库相对URL</param>
    /// <param name="bTime">开始时间</param>
    /// <param name="eTime">结束时间</param>
    /// <param name="Level">等级</param>
    /// <param name="Type">类型</param>
    /// <param name="UserID">用户ID</param>
    /// <param name="UserName">用户名</param>
    /// <param name="UserIP">访问IP</param>
    /// <param name="Message">消息</param>
    /// <param name="Data">数据</param>
    /// <returns></returns>
    public static List<Dictionary<string,object>> GetLogs2(string DbFilePath
            , string bTime = "", string eTime = "", string Type = "", string UserID = ""
            , string UserName = "", string UserIP = "", string Message = "", string Data = ""
        )
    {
        using (DataTable dt = LogDbHelper.GetLogs(DbFilePath, bTime, eTime, Type, UserID, UserName, UserIP, Message, Data)) {
            List<Dictionary<string, object>> table = new List<Dictionary<string, object>>();

            foreach (DataRow row in dt.Rows) {
                Dictionary<string, object> _row = new Dictionary<string, object>();
                _row["fileName"] = Path.GetFileName(DbFilePath);
                _row["filePath"] = DbFilePath;
                foreach (DataColumn dc in dt.Columns) {
                    _row[dc.ColumnName] = row[dc.ColumnName];
                }
                table.Add(_row);
            }
            return table;
        }
        
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