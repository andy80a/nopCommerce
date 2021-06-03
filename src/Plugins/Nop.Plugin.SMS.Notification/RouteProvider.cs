using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.SMS.Notification
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Plugin.SMS.Notification.Configure",
                 "Plugins/SmsNotification/Configure",
                 new { controller = "SMSNotificationController", action = "Configure" },
                 new[] { "Nop.Plugin.SMS.Notification.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
