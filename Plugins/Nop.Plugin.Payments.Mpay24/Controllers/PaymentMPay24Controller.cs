using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Plugin.Payments.MPay24;
using Nop.Plugin.Payments.MPay24.Models;
using Nop.Custom.Domain;
using Microsoft.AspNetCore.Http;

namespace Nop.Plugin.Payments.MPay24.Controllers
{
    public class PaymentMPay24Controller : BasePaymentController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly ICustomStoreService _customStoreService;
        private readonly IWorkContext _workContext;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public PaymentMPay24Controller(ILocalizationService localizationService,
            ILogger logger,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext,
            IStoreService storeService,
            IWorkContext workContext,
            IWebHelper webHelper,
            ICustomStoreService customStoreService)
        {
            this._localizationService = localizationService;
            this._logger = logger;
            this._orderProcessingService = orderProcessingService;
            this._orderService = orderService;
            this._permissionService = permissionService;
            this._settingService = settingService;
            this._storeContext = storeContext;
            this._storeService = storeService;
            this._workContext = workContext;
            this._webHelper = webHelper;
            this._customStoreService = customStoreService;
        }


        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var mPay24PaymentSettings = _settingService.LoadSetting<MPay24PaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                SoapUserName = mPay24PaymentSettings.SoapUserName,
                SoapPassword = mPay24PaymentSettings.SoapPassword,
                AdditionalFee = mPay24PaymentSettings.AdditionalFee,
                MerchantId = mPay24PaymentSettings.MerchantId,
                PaymentOptions = (string.IsNullOrEmpty(mPay24PaymentSettings.PaymentOptions) ? new string[] { } : mPay24PaymentSettings.PaymentOptions.Split(new char[] { ',' })),                            
                ActiveStoreScopeConfiguration = storeScope
            };
            IList<MPay24PaymentOptions> options = _customStoreService.GetAllMPay24PaymentOptions();
            model.PaymentOptionNames = new string[options.Count];
            for(int i = 0; i<options.Count; i++)
            {
                model.PaymentOptionNames[i] = options[i].DisplayName;
            }
            if (storeScope > 0)
            {
                model.SoapUserName_OverrideForStore = _settingService.SettingExists(mPay24PaymentSettings, x => x.SoapUserName, storeScope);
                model.SoapPassword_OverrideForStore = _settingService.SettingExists(mPay24PaymentSettings, x => x.SoapPassword, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(mPay24PaymentSettings, x => x.AdditionalFee, storeScope);
                model.MerchantId_OverrideForStore = _settingService.SettingExists(mPay24PaymentSettings, x => x.MerchantId, storeScope);
                model.PaymentOptions_OverrideForStore = _settingService.SettingExists(mPay24PaymentSettings, x => x.PaymentOptions, storeScope);
                model.PaymentOptionNames_OverrideForStore = false;
            }
            
            return View("~/Plugins/Payments.MPay24/Views/PaymentMPay24/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        [AuthorizeAdmin]
        [AdminAntiForgery]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var mPay24PaymentSettings = _settingService.LoadSetting<MPay24PaymentSettings>(storeScope);

            //save settings
            mPay24PaymentSettings.SoapUserName = model.SoapUserName;
            mPay24PaymentSettings.SoapPassword = model.SoapPassword;
            mPay24PaymentSettings.AdditionalFee = model.AdditionalFee;
            mPay24PaymentSettings.MerchantId = model.MerchantId;
            mPay24PaymentSettings.PaymentOptions = (model.PaymentOptions == null || model.PaymentOptions.Length == 0) ? "" : String.Join(",", model.PaymentOptions);



            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(mPay24PaymentSettings, x => x.SoapUserName, model.SoapUserName_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(mPay24PaymentSettings, x => x.SoapPassword, model.SoapPassword_OverrideForStore, storeScope, false);            
            _settingService.SaveSettingOverridablePerStore(mPay24PaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(mPay24PaymentSettings, x => x.MerchantId, model.MerchantId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(mPay24PaymentSettings, x => x.PaymentOptions, model.PaymentOptions_OverrideForStore, storeScope, false);
            

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

       

        //[ValidateInput(false)]
        public ActionResult PDTHandler()
        {
            int a = 10 / 10;
            if (a == 1) //((Controller)this).Request.Query
            {
                string orderGuid = ((Controller)this).Request.Query["orderId"];
                string transactionId = ((Controller)this).Request.Query["MPAYTID"];
                Order order = _orderService.GetOrderByGuid(Guid.Parse(orderGuid));
                string data = "";
                foreach (var queryItem in ((Controller)this).Request.Query)
                {
                    data += queryItem.Key + " = " + queryItem.Value + Environment.NewLine;
                }
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Information, "QueryString = " + data);
                //data = "";
                //foreach (string key in ((Controller)this).Request.Form.Keys)
                //{
                //    data += key + " = " + (((Controller)this).Request.Form[key]) + Environment.NewLine;
                //}
                //_logger.InsertLog(Core.Domain.Logging.LogLevel.Information, "Form = " + data);

                if (order == null || order.Id == 0)
                {
                    return RedirectToAction("Index", "Home", new { area = "" });
                }
                else
                {
                    //order note
                    if (!string.IsNullOrEmpty(transactionId))
                        order.AuthorizationTransactionId = transactionId;
                    order.OrderNotes.Add(new OrderNote()
                    {
                        Note = "Order Placed",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);

                    //mark order as paid
                    if (_orderProcessingService.CanMarkOrderAsPaid(order))
                    {
                        order.AuthorizationTransactionId = transactionId;
                        _orderService.UpdateOrder(order);

                        _orderProcessingService.MarkOrderAsPaid(order);
                    }

                    return this.RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
                }
            }
            else
            {                
                string orderNumber = ((Controller)this).Request.Query["orderId"];                
                var orderNumberGuid = Guid.Empty;
                try
                {
                    orderNumberGuid = new Guid(orderNumber);
                }
                catch { }
                var order = _orderService.GetOrderByGuid(orderNumberGuid);
                if (order != null)
                {
                    //order note
                    order.OrderNotes.Add(new OrderNote
                    {
                        Note = "MPay24 PDT failed. " + "Some Response here",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);
                }
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }


        //[ValidateInput(false)]
        public ActionResult IPNHandler()
        {
            _logger.InsertLog(Core.Domain.Logging.LogLevel.Information, "From IPN");
            try
            {
                byte[] parameters;
                using (var stream = new MemoryStream())
                {
                    this.Request.Body.CopyTo(stream);
                    parameters = stream.ToArray();
                }
                var strRequest = Encoding.ASCII.GetString(parameters);

                _logger.InsertLog(Core.Domain.Logging.LogLevel.Information, "IPN Data= " + strRequest);
            }
            catch (Exception) { }

            if (true) //((Controller)this).Request.QueryString
            {
                string orderGuid = ((Controller)this).Request.Query["orderId"];
                string transactionId = ((Controller)this).Request.Query["MPAYTID"];
                string status = ((Controller)this).Request.Query["STATUS"];
                Order order = _orderService.GetOrderByGuid(Guid.Parse(orderGuid));
                if (order != null && order.Id > 0 && !string.IsNullOrEmpty(status) && (status.ToUpperInvariant() == "BILLED" || status.ToUpperInvariant() == "RESERVED"))
                {
                    if (!string.IsNullOrEmpty(transactionId))
                        order.AuthorizationTransactionId = transactionId;
                    //order note
                    order.OrderNotes.Add(new OrderNote()
                    {
                        Note = "Order Billed - " + status,
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);

                    //mark order as paid
                    if (_orderProcessingService.CanMarkOrderAsPaid(order))
                    {
                        order.AuthorizationTransactionId = transactionId;
                        _orderService.UpdateOrder(order);

                        _orderProcessingService.MarkOrderAsPaid(order);
                    }
                }
            }
            return Content("");
        }

        public IActionResult CancelOrder()
        {
            var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1).FirstOrDefault();
            if (order != null)
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });

            return RedirectToRoute("HomePage");
        }
        #endregion
    }
}