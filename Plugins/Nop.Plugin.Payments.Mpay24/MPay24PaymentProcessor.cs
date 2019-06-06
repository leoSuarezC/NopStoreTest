using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tax;
using System.Net;
using Microsoft.Net.Http.Headers;
using System.IO;
using System.Text;
using MPay24Service;
using System.ServiceModel;
using Nop.Services.Stores;

namespace Nop.Plugin.Payments.MPay24
{
    /// <summary>
    /// PayPalDirect payment processor
    /// </summary>
    public class MPay24PaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICurrencyService _currencyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ISettingService _settingService;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly MPay24PaymentSettings _mPay24PaymentSettings;
        private readonly ICustomStoreService _customStoreService;
        #endregion

        #region Ctor

        public MPay24PaymentProcessor(CurrencySettings currencySettings,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            IOrderTotalCalculationService orderTotalCalculationService,
            ISettingService settingService,
            ITaxService taxService,
            IWebHelper webHelper,
            MPay24PaymentSettings mPay24PaymentSettings,
            ICustomStoreService customStoreService)
        {
            this._currencySettings = currencySettings;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._currencyService = currencyService;
            this._genericAttributeService = genericAttributeService;
            this._httpContextAccessor = httpContextAccessor;
            this._localizationService = localizationService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._settingService = settingService;
            this._taxService = taxService;
            this._webHelper = webHelper;
            this._mPay24PaymentSettings = mPay24PaymentSettings;
            this._customStoreService = customStoreService;
        }

        #endregion

        #region Methods
        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _mPay24PaymentSettings.AdditionalFee, false);
        }

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult();
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            string paymentType = "";
            string brand = "";
            string url = "";
            var options = _customStoreService.GetAllMPay24PaymentOptions();            
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].DisplayName.ToLower().Trim().Equals(postProcessPaymentRequest.MPay24PaymentOption.ToLower().Trim()))
                {
                    brand = options[i].Brand;
                    paymentType = options[i].PaymentType;
                    break;
                }
            }

            string brandQueryString = string.IsNullOrEmpty(brand) ? "" : (" Brand =\"" + brand + "\""); //A.V 30/09/2016, if Brand is empty in the DB, do not pass this parameter to MPay24

            //if (_mPay24PaymentSettings.MerchantId == 93010) //test account
            url = "https://test.mpay24.com/app/bin/etpproxy_v14";
            //url = "https://www.mpay24.com/app/bin/etpproxy_v14";

            BasicHttpBinding basicHttpBinding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
            basicHttpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            ChannelFactory<ETPChannel> factory = new ChannelFactory<ETPChannel>(basicHttpBinding, new EndpointAddress(new Uri(url)));
            factory.Credentials.UserName.UserName = "u93456";// "u73456";// _mPay24PaymentSettings.SoapUserName;
            factory.Credentials.UserName.Password = "xPbsxBJzdf";// "Ku >R6MkFsR";// _mPay24PaymentSettings.SoapPassword;
            ETPChannel serviceProxy = factory.CreateChannel();


            SelectPaymentResponse result = serviceProxy.SelectPaymentAsync(new SelectPaymentRequest()
            {
                merchantID = 93456,//73456,// (uint)_mPay24PaymentSettings.MerchantId,
                mdxi = "<Order PageStyle=\"background-color:#fff;border:1px solid #cccccc;\" PageHeaderStyle=\"background-color:#ebbc00;height:28px;\" PageCaptionStyle=\"background-color:#ebbc00;text-shadow: 0.1em 0.1em 0.2em #987900;padding-top:5px;\" ButtonsStyle=\"background-color:#CD271E;margin:5px;border-color: #6A6A6A #690901 #690901 #6A6A6A;border-style: solid;border-width: 1px;color: #FFFFFF;font: bold 12px Tahoma;\" Style=\"margin-left: auto; margin-right: auto; width:620px;font-size:13px;font-family:tahoma,sans-serif;\" LogoStyle=\"background:url(/client_img/745306160686e177ab93ecc62f543682.png) no-repeat;\" DropDownListsStyle=\"margin:3px 0;padding:3px;\" InputFieldsStyle=\"margin:3px 0;padding:3px;\"><Tid>" + postProcessPaymentRequest.Order.Id + "</Tid>"
                + ((!string.IsNullOrEmpty(paymentType)) ? "<PaymentTypes Enable=\"true\"><Payment Type=\"" + paymentType + "\" " + brandQueryString + "/></PaymentTypes>" : "")
                + "<Price>"
                + String.Format(CultureInfo.InvariantCulture, "{0:0.00}", postProcessPaymentRequest.Order.OrderTotal)
                + "</Price><URL><Success>" + String.Format("{0}Plugins/PaymentMPay24/PDTHandler/?orderId={1}", _webHelper.GetStoreLocation(false), postProcessPaymentRequest.Order.OrderGuid)
                + "</Success><Confirmation>" + String.Format("{0}Plugins/PaymentMPay24/IPNHandler/?orderId={1}", _webHelper.GetStoreLocation(false), postProcessPaymentRequest.Order.OrderGuid)
                + "</Confirmation><Cancel>" + String.Format("{0}Plugins/PaymentMPay24/CancelOrder/?orderId={1}", _webHelper.GetStoreLocation(false), postProcessPaymentRequest.Order.OrderGuid)
                + "</Cancel></URL></Order>",
                getDataURL = null,
                tid = null
            }).Result;

            // cleanup
            factory.Close();
            ((ICommunicationObject)serviceProxy).Close();

            if (result.@out.status == status.OK)
            {
                this._httpContextAccessor.HttpContext.Response.Redirect(result.@out.location);
            }
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

       

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            return new CapturePaymentResult { Errors = new[] { "Capture method not supported" } };
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            return new RefundPaymentResult { Errors = new[] { "Refund method not supported" } };
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult { Errors = new[] { "Void method not supported" } };
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return false;

            return true;
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            return new List<string>();
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentMPay24/Configure";
        }

        /// <summary>
        /// Gets a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <param name="viewComponentName">View component name</param>
        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = "PaymentMPay24";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new MPay24PaymentSettings
            {
                //UseSandbox = true
            });

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MPay24.RedirectionTip", "You will be redirected to MPay24 site to complete the order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MPay24.SoapUserName", "SOAP User Name");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MPay24.SoapUserName.Hint", "Enter SOAP User Name.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MPay24.SoapPassword", "SOAP Password");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MPay24.SoapPassword.Hint", "Enter SOAP Password.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MPay24.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MPay24.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MPay24.PaymentOptions", "Payment Options");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.MPay24.PaymentOptions.Hint", "Select accepted Payment Options.");
            
            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<MPay24PaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.MPay24.SoapUserName");
            this.DeletePluginLocaleResource("Plugins.Payments.MPay24.SoapUserName.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MPay24.SoapPassword");
            this.DeletePluginLocaleResource("Plugins.Payments.MPay24.SoapPassword.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MPay24.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.MPay24.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.MPay24.PaymentOptions");
            this.DeletePluginLocaleResource("Plugins.Payments.MPay24.PaymentOptions.Hint");
            
            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Redirection; }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
            get { return _localizationService.GetResource("Plugins.Payments.MPay24.PaymentMethodDescription"); }
        }

        #endregion
    }
}
