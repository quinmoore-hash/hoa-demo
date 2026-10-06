using System.Data.Entity;
using System.Web;
using System.Web.Http;
using HoaDemo.Api.Data;

namespace HoaDemo.Api
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer(new HoaDbInitializer());
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
