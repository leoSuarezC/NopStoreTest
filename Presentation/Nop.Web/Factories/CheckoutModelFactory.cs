﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Web.Models.Checkout;
using Nop.Web.Models.Common;

namespace Nop.Web.Factories
{
    public partial class CheckoutModelFactory : ICheckoutModelFactory
    {
        #region Fields

        private readonly IAddressModelFactory _addressModelFactory;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ILocalizationService _localizationService;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductService _productService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IShippingService _shippingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IRewardPointService _rewardPointService;
        private readonly IWebHelper _webHelper;

        private readonly CommonSettings _commonSettings;
        private readonly OrderSettings _orderSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly AddressSettings _addressSettings;
        private readonly ICustomStoreService _customStoreService;
        #endregion

        #region Ctor

        public CheckoutModelFactory(IAddressModelFactory addressModelFactory,
            IWorkContext workContext,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            ILocalizationService localizationService,
            ITaxService taxService,
            ICurrencyService currencyService,
            IPriceFormatter priceFormatter,
            IOrderProcessingService orderProcessingService,
            IProductAttributeParser productAttributeParser,
            IProductService productService,
            IGenericAttributeService genericAttributeService,
            ICountryService countryService,
            IStateProvinceService stateProvinceService,
            IShippingService shippingService,
            IPaymentService paymentService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IRewardPointService rewardPointService,
            IWebHelper webHelper,
            CommonSettings commonSettings,
            OrderSettings orderSettings,
            RewardPointsSettings rewardPointsSettings,
            PaymentSettings paymentSettings,
            ShippingSettings shippingSettings,
            AddressSettings addressSettings,
            ICustomStoreService customStoreService)
        {
            this._addressModelFactory = addressModelFactory;
            this._workContext = workContext;
            this._storeContext = storeContext;
            this._storeMappingService = storeMappingService;
            this._localizationService = localizationService;
            this._taxService = taxService;
            this._currencyService = currencyService;
            this._priceFormatter = priceFormatter;
            this._orderProcessingService = orderProcessingService;
            this._productAttributeParser = productAttributeParser;
            this._productService = productService;
            this._genericAttributeService = genericAttributeService;
            this._countryService = countryService;
            this._stateProvinceService = stateProvinceService;
            this._shippingService = shippingService;
            this._paymentService = paymentService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._rewardPointService = rewardPointService;
            this._webHelper = webHelper;

            this._commonSettings = commonSettings;
            this._orderSettings = orderSettings;
            this._rewardPointsSettings = rewardPointsSettings;
            this._paymentSettings = paymentSettings;
            this._shippingSettings = shippingSettings;
            this._addressSettings = addressSettings;

            this._customStoreService = customStoreService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare billing address model
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="selectedCountryId">Selected country identifier</param>
        /// <param name="prePopulateNewAddressWithCustomerFields">Pre populate new address with customer fields</param>
        /// <param name="overrideAttributesXml">Override attributes xml</param>
        /// <returns>Billing address model</returns>
        public virtual CheckoutBillingAddressModel PrepareBillingAddressModel(IList<ShoppingCartItem> cart,
            int? selectedCountryId = null,
            bool prePopulateNewAddressWithCustomerFields = false,
            string overrideAttributesXml = "")
        {
            var model = new CheckoutBillingAddressModel
            {
                ShipToSameAddressAllowed = _shippingSettings.ShipToSameAddress && cart.RequiresShipping(_productService, _productAttributeParser),
                //allow customers to enter (choose) a shipping address if "Disable Billing address step" setting is enabled
                ShipToSameAddress = !_orderSettings.DisableBillingAddressCheckoutStep
            };

            //existing addresses
            var addresses = _workContext.CurrentCustomer.Addresses
                .Where(a => a.Country == null ||
                    (//published
                    a.Country.Published &&
                    //allow billing
                    a.Country.AllowsBilling &&
                    //enabled for the current store
                    _storeMappingService.Authorize(a.Country)))
                .ToList();
            foreach (var address in addresses)
            {
                var addressModel = new AddressModel();
                _addressModelFactory.PrepareAddressModel(addressModel,
                    address: address,
                    excludeProperties: false,
                    addressSettings: _addressSettings);
                model.ExistingAddresses.Add(addressModel);
            }

            //new address
            model.BillingNewAddress.CountryId = selectedCountryId;
            _addressModelFactory.PrepareAddressModel(model.BillingNewAddress,
                address: null,
                excludeProperties: false,
                addressSettings: _addressSettings,
                loadCountries: () => _countryService.GetAllCountriesForBilling(_workContext.WorkingLanguage.Id),
                prePopulateWithCustomerFields: prePopulateNewAddressWithCustomerFields,
                customer: _workContext.CurrentCustomer,
                overrideAttributesXml: overrideAttributesXml);
            return model;
        }

        /// <summary>
        /// Prepare shipping address model
        /// </summary>
        /// <param name="selectedCountryId">Selected country identifier</param>
        /// <param name="prePopulateNewAddressWithCustomerFields">Pre populate new address with customer fields</param>
        /// <param name="overrideAttributesXml">Override attributes xml</param>
        /// <returns>Shipping address model</returns>
        public virtual CheckoutShippingAddressModel PrepareShippingAddressModel(int? selectedCountryId = null,
            bool prePopulateNewAddressWithCustomerFields = false, string overrideAttributesXml = "")
        {
            var model = new CheckoutShippingAddressModel
            {

                //allow pickup in store?
                AllowPickUpInStore = _shippingSettings.AllowPickUpInStore
            };
            if (model.AllowPickUpInStore)
            {
                model.DisplayPickupPointsOnMap = _shippingSettings.DisplayPickupPointsOnMap;
                model.GoogleMapsApiKey = _shippingSettings.GoogleMapsApiKey;
                var pickupPointProviders = _shippingService.LoadActivePickupPointProviders(_workContext.CurrentCustomer, _storeContext.CurrentStore.Id);
                if (pickupPointProviders.Any())
                {
                    var languageId = _workContext.WorkingLanguage.Id;
                    var pickupPointsResponse = _shippingService.GetPickupPoints(_workContext.CurrentCustomer.BillingAddress,
                        _workContext.CurrentCustomer, storeId: _storeContext.CurrentStore.Id);
                    if (pickupPointsResponse.Success)
                        model.PickupPoints = pickupPointsResponse.PickupPoints.Select(point =>
                        {
                            var country = _countryService.GetCountryByTwoLetterIsoCode(point.CountryCode);
                            var state = _stateProvinceService.GetStateProvinceByAbbreviation(point.StateAbbreviation, country?.Id);

                            var pickupPointModel = new CheckoutPickupPointModel
                            {
                                Id = point.Id,
                                Name = point.Name,
                                Description = point.Description,
                                ProviderSystemName = point.ProviderSystemName,
                                Address = point.Address,
                                City = point.City,
                                StateName = state?.GetLocalized(x => x.Name, languageId) ?? string.Empty,
                                CountryName = country?.GetLocalized(x => x.Name, languageId) ?? string.Empty,
                                ZipPostalCode = point.ZipPostalCode,
                                Latitude = point.Latitude,
                                Longitude = point.Longitude,
                                OpeningHours = point.OpeningHours
                            };
                            if (point.PickupFee > 0)
                            {
                                var amount = _taxService.GetShippingPrice(point.PickupFee, _workContext.CurrentCustomer);
                                amount = _currencyService.ConvertFromPrimaryStoreCurrency(amount, _workContext.WorkingCurrency);
                                pickupPointModel.PickupFee = _priceFormatter.FormatShippingPrice(amount, true);
                            }

                            return pickupPointModel;
                        }).ToList();
                    else
                        foreach (var error in pickupPointsResponse.Errors)
                            model.Warnings.Add(error);
                }

                //only available pickup points
                if (!_shippingService.LoadActiveShippingRateComputationMethods(_workContext.CurrentCustomer, _storeContext.CurrentStore.Id).Any())
                {
                    if (!pickupPointProviders.Any())
                    {
                        model.Warnings.Add(_localizationService.GetResource("Checkout.ShippingIsNotAllowed"));
                        model.Warnings.Add(_localizationService.GetResource("Checkout.PickupPoints.NotAvailable"));
                    }
                    model.PickUpInStoreOnly = true;
                    model.PickUpInStore = true;
                    return model;
                }
            }

            //existing addresses
            var addresses = _workContext.CurrentCustomer.Addresses
                .Where(a => a.Country == null ||
                    (//published
                    a.Country.Published &&
                    //allow shipping
                    a.Country.AllowsShipping &&
                    //enabled for the current store
                    _storeMappingService.Authorize(a.Country)))
                .ToList();
            foreach (var address in addresses)
            {
                var addressModel = new AddressModel();
                _addressModelFactory.PrepareAddressModel(addressModel,
                    address: address,
                    excludeProperties: false,
                    addressSettings: _addressSettings);
                model.ExistingAddresses.Add(addressModel);
            }

            //new address
            model.ShippingNewAddress.CountryId = selectedCountryId;
            _addressModelFactory.PrepareAddressModel(model.ShippingNewAddress,
                address: null,
                excludeProperties: false,
                addressSettings: _addressSettings,
                loadCountries: () => _countryService.GetAllCountriesForShipping(_workContext.WorkingLanguage.Id),
                prePopulateWithCustomerFields: prePopulateNewAddressWithCustomerFields,
                customer: _workContext.CurrentCustomer,
                overrideAttributesXml: overrideAttributesXml);

            return model;
        }

        /// <summary>
        /// Prepare shipping method model
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="shippingAddress">Shipping address</param>
        /// <returns>Shipping method model</returns>
        public virtual CheckoutShippingMethodModel PrepareShippingMethodModel(IList<ShoppingCartItem> cart, Address shippingAddress)
        {
            var model = new CheckoutShippingMethodModel();

            var getShippingOptionResponse = _shippingService.GetShippingOptions(cart, shippingAddress, _workContext.CurrentCustomer, storeId: _storeContext.CurrentStore.Id);
            if (getShippingOptionResponse.Success)
            {
                //performance optimization. cache returned shipping options.
                //we'll use them later (after a customer has selected an option).
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
                                                       SystemCustomerAttributeNames.OfferedShippingOptions,
                                                       getShippingOptionResponse.ShippingOptions,
                                                       _storeContext.CurrentStore.Id);

                foreach (var shippingOption in getShippingOptionResponse.ShippingOptions)
                {
                    var soModel = new CheckoutShippingMethodModel.ShippingMethodModel
                    {
                        Name = shippingOption.Name,
                        Description = shippingOption.Description,
                        ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName,
                        ShippingOption = shippingOption,
                    };

                    //adjust rate
                    var shippingTotal = _orderTotalCalculationService.AdjustShippingRate(shippingOption.Rate, cart, out List<DiscountForCaching> _);

                    var rateBase = _taxService.GetShippingPrice(shippingTotal, _workContext.CurrentCustomer);
                    var rate = _currencyService.ConvertFromPrimaryStoreCurrency(rateBase, _workContext.WorkingCurrency);
                    soModel.Fee = _priceFormatter.FormatShippingPrice(rate, true);

                    model.ShippingMethods.Add(soModel);
                }

                //find a selected (previously) shipping method
                var selectedShippingOption = _workContext.CurrentCustomer.GetAttribute<ShippingOption>(
                        SystemCustomerAttributeNames.SelectedShippingOption, _storeContext.CurrentStore.Id);
                if (selectedShippingOption != null)
                {
                    var shippingOptionToSelect = model.ShippingMethods.ToList()
                        .Find(so =>
                           !string.IsNullOrEmpty(so.Name) &&
                           so.Name.Equals(selectedShippingOption.Name, StringComparison.InvariantCultureIgnoreCase) &&
                           !string.IsNullOrEmpty(so.ShippingRateComputationMethodSystemName) &&
                           so.ShippingRateComputationMethodSystemName.Equals(selectedShippingOption.ShippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase));
                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }
                //if no option has been selected, let's do it for the first one
                if (model.ShippingMethods.FirstOrDefault(so => so.Selected) == null)
                {
                    var shippingOptionToSelect = model.ShippingMethods.FirstOrDefault();
                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }

                //notify about shipping from multiple locations
                if (_shippingSettings.NotifyCustomerAboutShippingFromMultipleLocations)
                {
                    model.NotifyCustomerAboutShippingFromMultipleLocations = getShippingOptionResponse.ShippingFromMultipleLocations;
                }
            }
            else
            {
                foreach (var error in getShippingOptionResponse.Errors)
                    model.Warnings.Add(error);
            }

            return model;
        }

        /// <summary>
        /// Prepare payment method model
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="filterByCountryId">Filter by country identifier</param>
        /// <returns>Payment method model</returns>
        public virtual CheckoutPaymentMethodModel PreparePaymentMethodModel(IList<ShoppingCartItem> cart, int filterByCountryId)
        {
            var mpay24PaymentOptions = _customStoreService.GetSelectedMPay24PaymentOptions();
            var model = new CheckoutPaymentMethodModel();

            //reward points
            if (_rewardPointsSettings.Enabled && !cart.IsRecurring())
            {
                var rewardPointsBalance = _rewardPointService.GetRewardPointsBalance(_workContext.CurrentCustomer.Id, _storeContext.CurrentStore.Id);
                var rewardPointsAmountBase = _orderTotalCalculationService.ConvertRewardPointsToAmount(rewardPointsBalance);
                var rewardPointsAmount = _currencyService.ConvertFromPrimaryStoreCurrency(rewardPointsAmountBase, _workContext.WorkingCurrency);
                if (rewardPointsAmount > decimal.Zero &&
                    _orderTotalCalculationService.CheckMinimumRewardPointsToUseRequirement(rewardPointsBalance))
                {
                    model.DisplayRewardPoints = true;
                    model.RewardPointsAmount = _priceFormatter.FormatPrice(rewardPointsAmount, true, false);
                    model.RewardPointsBalance = rewardPointsBalance;

                    //are points enough to pay for entire order? like if this option (to use them) was selected
                    model.RewardPointsEnoughToPayForOrder = !_orderProcessingService.IsPaymentWorkflowRequired(cart, true);
                }
            }

            //filter by country
            var paymentMethods = _paymentService
                .LoadActivePaymentMethods(_workContext.CurrentCustomer, _storeContext.CurrentStore.Id, filterByCountryId)
                .Where(pm => pm.PaymentMethodType == PaymentMethodType.Standard || pm.PaymentMethodType == PaymentMethodType.Redirection)
                .Where(pm => !pm.HidePaymentMethod(cart))
                .ToList();
            foreach (var pm in paymentMethods)
            {
                if (cart.IsRecurring() && pm.RecurringPaymentType == RecurringPaymentType.NotSupported)
                    continue;
                if (pm.PluginDescriptor.SystemName == "Payments.MPay24")//A.V
                {
                    foreach (var mpay24Option in mpay24PaymentOptions)
                    {
                        var pmModel = new CheckoutPaymentMethodModel.PaymentMethodModel
                        {
                            Name = mpay24Option.DisplayName,
                            Description = _paymentSettings.ShowPaymentMethodDescriptions ? mpay24Option.Description : string.Empty,
                            PaymentMethodSystemName = pm.PluginDescriptor.SystemName,
                            LogoUrl = mpay24Option.Logo
                        };
                        //payment method additional fee
                        var paymentMethodAdditionalFee = _paymentService.GetAdditionalHandlingFee(cart, pm.PluginDescriptor.SystemName);
                        var rateBase = _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, _workContext.CurrentCustomer);
                        var rate = _currencyService.ConvertFromPrimaryStoreCurrency(rateBase, _workContext.WorkingCurrency);
                        if (rate > decimal.Zero)
                            pmModel.Fee = _priceFormatter.FormatPaymentMethodAdditionalFee(rate, true);

                        model.PaymentMethods.Add(pmModel);
                    }
                }
                else
                {
                    var pmModel = new CheckoutPaymentMethodModel.PaymentMethodModel
                    {
                        Name = pm.GetLocalizedFriendlyName(_localizationService, _workContext.WorkingLanguage.Id),
                        Description = _paymentSettings.ShowPaymentMethodDescriptions ? pm.PaymentMethodDescription : string.Empty,
                        PaymentMethodSystemName = pm.PluginDescriptor.SystemName,
                        LogoUrl = pm.PluginDescriptor.GetLogoUrl(_webHelper)
                    };
                    //payment method additional fee
                    var paymentMethodAdditionalFee = _paymentService.GetAdditionalHandlingFee(cart, pm.PluginDescriptor.SystemName);
                    var rateBase = _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, _workContext.CurrentCustomer);
                    var rate = _currencyService.ConvertFromPrimaryStoreCurrency(rateBase, _workContext.WorkingCurrency);
                    if (rate > decimal.Zero)
                        pmModel.Fee = _priceFormatter.FormatPaymentMethodAdditionalFee(rate, true);

                    model.PaymentMethods.Add(pmModel);
                }
            }

            //find a selected (previously) payment method
            var selectedPaymentMethodSystemName = _workContext.CurrentCustomer.GetAttribute<string>(
                SystemCustomerAttributeNames.SelectedPaymentMethod,
                _genericAttributeService, _storeContext.CurrentStore.Id);
            if (!string.IsNullOrEmpty(selectedPaymentMethodSystemName))
            {
                var paymentMethodToSelect = model.PaymentMethods.ToList()
                    .Find(pm => pm.PaymentMethodSystemName.Equals(selectedPaymentMethodSystemName, StringComparison.InvariantCultureIgnoreCase));
                if (paymentMethodToSelect != null)
                    paymentMethodToSelect.Selected = true;
            }
            //if no option has been selected, let's do it for the first one
            if (model.PaymentMethods.FirstOrDefault(so => so.Selected) == null)
            {
                var paymentMethodToSelect = model.PaymentMethods.FirstOrDefault();
                if (paymentMethodToSelect != null)
                    paymentMethodToSelect.Selected = true;
            }

            return model;
        }

        /// <summary>
        /// Prepare payment info model
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <returns>Payment info model</returns>
        public virtual CheckoutPaymentInfoModel PreparePaymentInfoModel(IPaymentMethod paymentMethod)
        {
            paymentMethod.GetPublicViewComponent(out string viewComponentName);

            var model = new CheckoutPaymentInfoModel
            {
                PaymentViewComponentName = viewComponentName,
                DisplayOrderTotals = _orderSettings.OnePageCheckoutDisplayOrderTotalsOnPaymentInfoTab
            };

            return model;
        }

        /// <summary>
        /// Prepare confirm order model
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <returns>Confirm order model</returns>
        public virtual CheckoutConfirmModel PrepareConfirmOrderModel(IList<ShoppingCartItem> cart)
        {
            var model = new CheckoutConfirmModel
            {
                //terms of service
                TermsOfServiceOnOrderConfirmPage = _orderSettings.TermsOfServiceOnOrderConfirmPage,
                TermsOfServicePopup = _commonSettings.PopupForTermsOfServiceLinks
            };
            //min order amount validation
            var minOrderTotalAmountOk = _orderProcessingService.ValidateMinOrderTotalAmount(cart);
            if (!minOrderTotalAmountOk)
            {
                var minOrderTotalAmount = _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderTotalAmount, _workContext.WorkingCurrency);
                model.MinOrderTotalWarning = string.Format(_localizationService.GetResource("Checkout.MinOrderTotalAmount"), _priceFormatter.FormatPrice(minOrderTotalAmount, true, false));
            }
            return model;
        }

        /// <summary>
        /// Prepare checkout completed model
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Checkout completed model</returns>
        public virtual CheckoutCompletedModel PrepareCheckoutCompletedModel(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var model = new CheckoutCompletedModel
            {
                OrderId = order.Id,
                OnePageCheckoutEnabled = _orderSettings.OnePageCheckoutEnabled,
                CustomOrderNumber = order.CustomOrderNumber,
                CustomerId = order.CustomerId
            };

            return model;
        }

        /// <summary>
        /// Prepare checkout progress model
        /// </summary>
        /// <param name="step">Step</param>
        /// <returns>Checkout progress model</returns>
        public virtual CheckoutProgressModel PrepareCheckoutProgressModel(CheckoutProgressStep step)
        {
            var model = new CheckoutProgressModel { CheckoutProgressStep = step };
            return model;
        }

        /// <summary>
        /// Prepare one page checkout model
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <returns>One page checkout model</returns>
        public virtual OnePageCheckoutModel PrepareOnePageCheckoutModel(IList<ShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            var model = new OnePageCheckoutModel
            {
                ShippingRequired = cart.RequiresShipping(_productService, _productAttributeParser),
                DisableBillingAddressCheckoutStep = _orderSettings.DisableBillingAddressCheckoutStep,
                BillingAddress = PrepareBillingAddressModel(cart, prePopulateNewAddressWithCustomerFields: true)
            };
            return model;
        }

        #endregion
    }
}
