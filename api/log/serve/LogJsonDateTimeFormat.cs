
/// <summary>
/// Json日期格式化
/// </summary>
public class LogJsonDateTimeFormat : System.Web.Http.Filters.ActionFilterAttribute
{
    private string FormatString;
    public LogJsonDateTimeFormat(){
        FormatString = "yyyy-MM-dd HH:mm:ss";
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="FormatString">日期格式，如 yyyy-MM-dd HH:mm:ss</param>
    public LogJsonDateTimeFormat(string FormatString) {
        this.FormatString = FormatString;
    }

    public override void OnActionExecuting(System.Web.Http.Controllers.HttpActionContext actionContext)
    {
        var JsonFormatter = actionContext.ControllerContext.Configuration.Formatters.JsonFormatter;
        if (JsonFormatter != null)
        {
            JsonFormatter.SerializerSettings.DateFormatString = FormatString;
            
        }

        base.OnActionExecuting(actionContext);
    }
}

