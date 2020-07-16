using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API 配置和服务

            // Web API 路由
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            #region 开启Session

            // 开启全局跨域
            var cor = new System.Web.Http.Cors.EnableCorsAttribute("*", "*", "*");
            // 允许cookie发送，否则session不可用
            cor.SupportsCredentials = true;
            config.EnableCors(cor);

            #endregion

            //清除默认xml,使api返回为json 
            GlobalConfiguration.Configuration.Formatters.XmlFormatter.SupportedMediaTypes.Clear();
            //通过参数设置返回格式
            //GlobalConfiguration.Configuration.Formatters.JsonFormatter.MediaTypeMappings.Add(new QueryStringMapping("t", "json", "application/json"));
            //GlobalConfiguration.Configuration.Formatters.XmlFormatter.MediaTypeMappings.Add(new QueryStringMapping("t", "xml", "application/xml"));
            //这里用到了QueryStringMapping 类，并传递给他构造函数的参数 new QueryStringMapping("t", "json", "application/json") 
            //和 new QueryStringMapping("t", "xml", "application/xml") 这样就分别对用了json和xml，而前面的“t”表示请求的url地址
            //中参数名称t，下面我们分别来测试下两种请求的参数，地址分别为http://localhost:1001/s/all01?t=xml



        }
    }
}
