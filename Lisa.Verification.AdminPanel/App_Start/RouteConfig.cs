using System.Web.Mvc;
using System.Web.Routing;

namespace Lisa.Verification.AdminPanel
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Login", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Applications",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Application", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
