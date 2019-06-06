using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.MPay24.Components
{
    [ViewComponent(Name = "PaymentMPay24")]
    public class PaymentMPay24ViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.MPay24/Views/PaymentMPay24/PaymentInfo.cshtml");
        }
    }
}
