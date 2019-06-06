using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Mvc.Models;


namespace Nop.Plugin.Payments.MPay24.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MPay24.SoapUserName")]
        public string SoapUserName { get; set; }
        public bool SoapUserName_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MPay24.SoapPassword")]
        public string SoapPassword { get; set; }
        public bool SoapPassword_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MPay24.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MPay24.MerchantId")]
        public int MerchantId { get; set; }
        public bool MerchantId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.MPay24.PaymentOptions")]
        public string []PaymentOptions { get; set; }
        public bool PaymentOptions_OverrideForStore { get; set; }

        public string[] PaymentOptionNames { get; set; }
        public bool PaymentOptionNames_OverrideForStore { get; set; }
    }
}