using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;

namespace api
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        /// <summary>
        /// ³õÊ¼»¯¿ªÆôSession
        /// </summary>
        public override void Init()
        {
            PostAuthenticateRequest += (sender, e) =>
            {
                HttpContext.Current.SetSessionStateBehavior(System.Web.SessionState.SessionStateBehavior.Required);
            };
            base.Init();
        }

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
