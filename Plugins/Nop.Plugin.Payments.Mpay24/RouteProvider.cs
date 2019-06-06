using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.MPay24
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IRouteBuilder routes)
        {
            routes.MapRoute("Plugin.Payments.MPay24.Configure",
                 "Plugins/PaymentMPay24/Configure",
                 new { controller = "PaymentMPay24", action = "Configure" }
            );

            routes.MapRoute("Plugin.Payments.MPay24.PaymentInfo",
                 "Plugins/PaymentMPay24/PaymentInfo",
                 new { controller = "PaymentMPay24", action = "PaymentInfo" }
            );

            //IPN
            routes.MapRoute("Plugin.Payments.MPay24.IPNHandler",
                 "Plugins/PaymentMPay24/IPNHandler",
                 new { controller = "PaymentMPay24", action = "IPNHandler" }
            );
            
            routes.MapRoute("Plugin.Payments.MPay24.PDTHandler",
                 "Plugins/PaymentMPay24/PDTHandler",
                 new { controller = "PaymentMPay24", action = "PDTHandler" }
            );

            routes.MapRoute("Plugin.Payments.MPay24.CancelOrder",
                 "Plugins/PaymentMPay24/CancelOrder",
                 new { controller = "PaymentMPay24", action = "CancelOrder" }
            );
        }
        public int Priority
        {
            get
            {
                return -1;
            }
        }
    }
}
