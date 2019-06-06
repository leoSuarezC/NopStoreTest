using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.CashOnDelivery.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.CashOnDelivery.Components
{
    [ViewComponent(Name = "CashOnDelivery")]
    public class CashOnDeliveryViewComponent : NopViewComponent
    {
        private readonly CashOnDeliveryPaymentSettings _cashOnDeliveryPaymentSettings;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        public CashOnDeliveryViewComponent(CashOnDeliveryPaymentSettings cashOnDeliveryPaymentSettings,
            IStoreContext storeContext,
            IWorkContext workContext)
        {
            this._cashOnDeliveryPaymentSettings = cashOnDeliveryPaymentSettings;
            this._storeContext = storeContext;
            this._workContext = workContext;
        }

        public IViewComponentResult Invoke()
        {
            var model = new PaymentInfoModel
            {
                DescriptionText = _cashOnDeliveryPaymentSettings.GetLocalizedSetting(x => x.DescriptionText,
                    _workContext.WorkingLanguage.Id, _storeContext.CurrentStore.Id)
            };

            return View("~/Plugins/Payments.CashOnDelivery/Views/PaymentInfo.cshtml", model);
        }
    }
}
