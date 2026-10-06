using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace HoaDemo.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });

            var json = config.Formatters.JsonFormatter.SerializerSettings;
            json.ContractResolver = new CamelCasePropertyNamesContractResolver();
            json.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            json.Converters.Add(new StringEnumConverter());

            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}
