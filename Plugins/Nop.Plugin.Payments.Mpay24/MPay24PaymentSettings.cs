using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.MPay24
{
    public class MPay24PaymentSettings : ISettings
    {
        public string SoapUserName { get; set; }
        public string SoapPassword { get; set; }
        public decimal AdditionalFee { get; set; }
        public int MerchantId { get; set; }
        public string PaymentOptions { get; set; }
    }
}
