using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Vendors;
using Nop.Custom.Domain;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Factories;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework.Security;
using Nop.Web.Models.Catalog;
using Omikron.FactFinder.Adapter;
using Omikron.FactFinder.Core.Client;
using Omikron.FactFinder.Core.Server;

namespace Nop.Web.Controllers
{
    public partial class CatalogController : BasePublicController
    {
        #region Fields
        private readonly IPictureService _pictureService;
        private readonly ICatalogModelFactory _catalogModelFactory;
        private readonly IProductModelFactory _productModelFactory;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductService _productService;
        private readonly IVendorService _vendorService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        private readonly IProductTagService _productTagService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IPermissionService _permissionService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly MediaSettings _mediaSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly VendorSettings _vendorSettings;
        private readonly ICustomStoreService _customStoreService;
        #endregion

        #region Ctor

        public CatalogController(ICatalogModelFactory catalogModelFactory,
            IProductModelFactory productModelFactory,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            IProductService productService,
            IVendorService vendorService,
            IWorkContext workContext,
            IStoreContext storeContext,
            ILocalizationService localizationService,
            IWebHelper webHelper,
            IProductTagService productTagService,
            IGenericAttributeService genericAttributeService,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            IPermissionService permissionService,
            ICustomerActivityService customerActivityService,
            MediaSettings mediaSettings,
            CatalogSettings catalogSettings,
            VendorSettings vendorSettings,
            ICustomStoreService customStoreService,
            IPictureService pictureService)
        {
            this._catalogModelFactory = catalogModelFactory;
            this._productModelFactory = productModelFactory;
            this._categoryService = categoryService;
            this._manufacturerService = manufacturerService;
            this._productService = productService;
            this._vendorService = vendorService;
            this._workContext = workContext;
            this._storeContext = storeContext;
            this._localizationService = localizationService;
            this._webHelper = webHelper;
            this._productTagService = productTagService;
            this._genericAttributeService = genericAttributeService;
            this._aclService = aclService;
            this._storeMappingService = storeMappingService;
            this._permissionService = permissionService;
            this._customerActivityService = customerActivityService;
            this._mediaSettings = mediaSettings;
            this._catalogSettings = catalogSettings;
            this._vendorSettings = vendorSettings;
            this._customStoreService = customStoreService;
            this._pictureService = pictureService;
        }

        #endregion

        #region Categories

        [HttpsRequirement(SslRequirement.No)]
        public virtual IActionResult Category(int categoryId, CatalogPagingFilteringModel command)
        {
            var category = _categoryService.GetCategoryById(categoryId);
            if (category == null || category.Deleted)
                return InvokeHttp404();

            var notAvailable =
                //published?
                !category.Published ||
                //ACL (access control list) 
                !_aclService.Authorize(category) ||
                //Store mapping
                !_storeMappingService.Authorize(category);
            //Check whether the current user has a "Manage categories" permission (usually a store owner)
            //We should allows him (her) to use "Preview" functionality
            if (notAvailable && !_permissionService.Authorize(StandardPermissionProvider.ManageCategories))
                return InvokeHttp404();

            //'Continue shopping' URL
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
                SystemCustomerAttributeNames.LastContinueShoppingPage,
                _webHelper.GetThisPageUrl(false),
                _storeContext.CurrentStore.Id);

            //display "edit" (manage) link
            if (_permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel) && _permissionService.Authorize(StandardPermissionProvider.ManageCategories))
                DisplayEditLink(Url.Action("Edit", "Category", new { id = category.Id, area = AreaNames.Admin }));

            //activity log
            _customerActivityService.InsertActivity("PublicStore.ViewCategory", _localizationService.GetResource("ActivityLog.PublicStore.ViewCategory"), category.Name);

            //model
            var model = _catalogModelFactory.PrepareCategoryModel(category, command);

            //template
            var templateViewPath = _catalogModelFactory.PrepareCategoryTemplateViewPath(category.CategoryTemplateId);
            return View(templateViewPath, model);
        }

        #endregion

        #region Manufacturers

        [HttpsRequirement(SslRequirement.No)]
        public virtual IActionResult Manufacturer(int manufacturerId, CatalogPagingFilteringModel command)
        {
            var manufacturer = _manufacturerService.GetManufacturerById(manufacturerId);
            if (manufacturer == null || manufacturer.Deleted)
                return InvokeHttp404();

            var notAvailable =
                //published?
                !manufacturer.Published ||
                //ACL (access control list) 
                !_aclService.Authorize(manufacturer) ||
                //Store mapping
                !_storeMappingService.Authorize(manufacturer);
            //Check whether the current user has a "Manage categories" permission (usually a store owner)
            //We should allows him (her) to use "Preview" functionality
            if (notAvailable && !_permissionService.Authorize(StandardPermissionProvider.ManageManufacturers))
                return InvokeHttp404();

            //'Continue shopping' URL
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
                SystemCustomerAttributeNames.LastContinueShoppingPage,
                _webHelper.GetThisPageUrl(false),
                _storeContext.CurrentStore.Id);

            //display "edit" (manage) link
            if (_permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel) && _permissionService.Authorize(StandardPermissionProvider.ManageManufacturers))
                DisplayEditLink(Url.Action("Edit", "Manufacturer", new { id = manufacturer.Id, area = AreaNames.Admin }));

            //activity log
            _customerActivityService.InsertActivity("PublicStore.ViewManufacturer", _localizationService.GetResource("ActivityLog.PublicStore.ViewManufacturer"), manufacturer.Name);

            //model
            var model = _catalogModelFactory.PrepareManufacturerModel(manufacturer, command);

            //template
            var templateViewPath = _catalogModelFactory.PrepareManufacturerTemplateViewPath(manufacturer.ManufacturerTemplateId);
            return View(templateViewPath, model);
        }

        [HttpsRequirement(SslRequirement.No)]
        public virtual IActionResult ManufacturerAll()
        {
            ViewBag.ManufacturerSeoMap = _customStoreService.GetManufacturerSeoMapping();
            var model = _catalogModelFactory.PrepareManufacturerAllModels();
            return View(model);
        }

        #endregion

        #region Vendors

        [HttpsRequirement(SslRequirement.No)]
        public virtual IActionResult Vendor(int vendorId, CatalogPagingFilteringModel command)
        {
            var vendor = _vendorService.GetVendorById(vendorId);
            if (vendor == null || vendor.Deleted || !vendor.Active)
                return InvokeHttp404();

            //'Continue shopping' URL
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
                SystemCustomerAttributeNames.LastContinueShoppingPage,
                _webHelper.GetThisPageUrl(false),
                _storeContext.CurrentStore.Id);

            //display "edit" (manage) link
            if (_permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel) && _permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                DisplayEditLink(Url.Action("Edit", "Vendor", new { id = vendor.Id, area = AreaNames.Admin }));

            //model
            var model = _catalogModelFactory.PrepareVendorModel(vendor, command);

            return View(model);
        }

        [HttpsRequirement(SslRequirement.No)]
        public virtual IActionResult VendorAll()
        {
            //we don't allow viewing of vendors if "vendors" block is hidden
            if (_vendorSettings.VendorsBlockItemsToDisplay == 0)
                return RedirectToRoute("HomePage");

            var model = _catalogModelFactory.PrepareVendorAllModels();
            return View(model);
        }

        #endregion

        #region Product tags

        [HttpsRequirement(SslRequirement.No)]
        public virtual IActionResult ProductsByTag(int productTagId, CatalogPagingFilteringModel command)
        {
            var productTag = _productTagService.GetProductTagById(productTagId);
            if (productTag == null)
                return InvokeHttp404();

            var model = _catalogModelFactory.PrepareProductsByTagModel(productTag, command);
            return View(model);
        }

        [HttpsRequirement(SslRequirement.No)]
        public virtual IActionResult ProductTagsAll()
        {
            var model = _catalogModelFactory.PrepareProductTagsAllModel();
            return View(model);
        }

        #endregion

        #region Searching

        [HttpsRequirement(SslRequirement.No)]
        public virtual IActionResult Search(SearchModel model, CatalogPagingFilteringModel command)
        {
            //'Continue shopping' URL
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
                SystemCustomerAttributeNames.LastContinueShoppingPage,
                _webHelper.GetThisPageUrl(false),
                _storeContext.CurrentStore.Id);

            if (model == null)
                model = new SearchModel();

            model = _catalogModelFactory.PrepareSearchModel(model, command);
            return View(model);
        }
        [HttpsRequirement(SslRequirement.No)]
        public virtual IActionResult SearchFactFinder(string ProduktOberGruppeName, string ProduktGruppeName, string ProduktUnterGruppeName,
            string ProduktUnterGruppeKat4, string ProduktUnterGruppeKat5,
            string[] ProduktMarkeName,
            string[] Farbe,
            string AngebotArt, string query, string Seite)
        {
            //ProduktOberGruppeName = category 1
            //ProduktGruppeName = category 2
            //ProduktUnterGruppeName = category 3
            //ProduktUnterGruppeKat4 = category 4
            //ProduktUnterGruppeKat5 = category 5
            //ProduktMarkeName = make
            //Farbe = color
            //AngebotArt = Special offers aka Sonderangebote
            NameValueCollection nvc = new NameValueCollection();
            if (!string.IsNullOrEmpty(ProduktOberGruppeName))
                nvc.Add("filterKategorie1", ProduktOberGruppeName);

            if (!string.IsNullOrEmpty(ProduktGruppeName))
                nvc.Add("filterKategorie2", ProduktGruppeName);

            if (!string.IsNullOrEmpty(ProduktUnterGruppeName))
                nvc.Add("filterKategorie3", ProduktUnterGruppeName);

            if (!string.IsNullOrEmpty(ProduktUnterGruppeKat4))
                nvc.Add("filterKategorie4", ProduktUnterGruppeKat4);

            if (!string.IsNullOrEmpty(ProduktUnterGruppeKat5))
                nvc.Add("filterKategorie5", ProduktUnterGruppeKat5);

            if (ProduktMarkeName != null && ProduktMarkeName.Length > 0)
                nvc.Add("filterMarke", ProduktMarkeName[0]);

            if (Farbe != null && Farbe.Length > 0)
            {
                string colors = "";
                foreach (var farbe in Farbe)
                {
                    colors += farbe + "~~~";
                }
                nvc.Add("filterFilterFarbe", colors.TrimEnd('~'));
            }

            if (!string.IsNullOrEmpty(AngebotArt))
                nvc.Add("filterAngebotArt", AngebotArt);

            if (!string.IsNullOrEmpty(Seite))
                nvc.Add("page", Seite);

            if (string.IsNullOrEmpty(query))
                nvc.Add("query", "*");
            else
                nvc.Add("query", query);
            ViewBag.CategorySeoMap = _customStoreService.GetCategorySeoMapping();
            ViewBag.SeoInfoModel = _customStoreService.GetSeoInfo(nvc, (IList<SeoMap>)ViewBag.CategorySeoMap);

            string category = string.Empty;
            int categoryLevel = 0;
            for (int i = 0; i < nvc.Count; i++)
            {
                if (nvc.GetKey(i) == "filterKategorie5")
                {
                    category = nvc.GetValues(i).FirstOrDefault();
                    categoryLevel = 5;
                }
                else if (nvc.GetKey(i) == "filterKategorie4" && categoryLevel < 4)
                {
                    category = nvc.GetValues(i).FirstOrDefault();
                    categoryLevel = 4;
                }
                else if (nvc.GetKey(i) == "filterKategorie3" && categoryLevel < 3)
                {
                    category = nvc.GetValues(i).FirstOrDefault();
                    categoryLevel = 3;
                }
                else if (nvc.GetKey(i) == "filterKategorie2" && categoryLevel < 2)
                {
                    category = nvc.GetValues(i).FirstOrDefault();
                    categoryLevel = 2;
                }
                else if (nvc.GetKey(i) == "filterKategorie1" && categoryLevel < 1)
                {
                    category = nvc.GetValues(i).FirstOrDefault();
                    categoryLevel = 1;
                }

                var seoText = nvc.GetValues(i).FirstOrDefault();
                var item = ((IList<SeoMap>)ViewBag.CategorySeoMap).Where(t => t.text_seo == seoText).FirstOrDefault();
                if (item != null)
                {
                    if (nvc.GetKey(i) == "filterKategorie5" && categoryLevel == 5)
                    {
                        category = item.text_orig;
                    }
                    else if (nvc.GetKey(i) == "filterKategorie4" && categoryLevel == 4)
                    {
                        category = item.text_orig;
                    }
                    else if (nvc.GetKey(i) == "filterKategorie3" && categoryLevel == 3)
                    {
                        category = item.text_orig;
                    }
                    else if (nvc.GetKey(i) == "filterKategorie2" && categoryLevel == 2)
                    {
                        category = item.text_orig;
                    }
                    else if (nvc.GetKey(i) == "filterKategorie1" && categoryLevel == 1)
                    {
                        category = item.text_orig;
                    }
                }
                ViewBag.CategoryPictureUrl = "https://dev.auner.at/Themes/Venture/Content/img/h1-background.jpg";
                Category currentCategory = _categoryService.GetAllCategories(category).FirstOrDefault();
                if (currentCategory.PictureId > 0)
                {
                    ViewBag.CategoryPictureUrl = _pictureService.GetPictureUrl(currentCategory.PictureId);
                }
            }
            return View("Search", nvc);
        }

        public bool TrackingProxy()
        {
            Omikron.FactFinder.Util.HttpContextFactory.Current = HttpContext;
            var requestParser = new RequestParser();
            var requestParameters = requestParser.RequestParameters;
            var requestFactory = new HttpRequestFactory(requestParameters);
            var clientUrlBuilder = new Omikron.FactFinder.Core.Client.UrlBuilder(requestParser);
            var trackingAdapter = new Tracking(requestFactory.GetRequest(), clientUrlBuilder);
            return trackingAdapter.DoTrackingFromRequest();
            //return FactFinderSearchEngine.CallTrackingService(id, channel, sid, @event, masterid, title, userid, query, pos, origPos, page, origPageSize, pagesize, count, price, simi, mainid);
        }
        public virtual IActionResult SearchTermAutoComplete(string term, string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < _catalogSettings.ProductSearchTermMinimumLength)
                return Content("");

            //if (string.IsNullOrWhiteSpace(term) || term.Length < _catalogSettings.ProductSearchTermMinimumLength)
            //    return Content("");

            ////products
            //var productNumber = _catalogSettings.ProductSearchAutoCompleteNumberOfProducts > 0 ?
            //    _catalogSettings.ProductSearchAutoCompleteNumberOfProducts : 10;

            //var products = _productService.SearchProducts(
            //    storeId: _storeContext.CurrentStore.Id,
            //    keywords: term,
            //    languageId: _workContext.WorkingLanguage.Id,
            //    visibleIndividuallyOnly: true,
            //    pageSize: productNumber);

            //var models =  _productModelFactory.PrepareProductOverviewModels(products, false, _catalogSettings.ShowProductImagesInSearchAutoComplete, _mediaSettings.AutoCompleteSearchThumbPictureSize).ToList();
            //var result = (from p in models
            //        select new
            //        {
            //            label = p.Name,
            //            producturl = Url.RouteUrl("Product", new {SeName = p.SeName}),
            //            productpictureurl = p.DefaultPictureModel.ImageUrl
            //        })
            //    .ToList();
            //return Json(result);
            Omikron.FactFinder.Util.HttpContextFactory.Current = HttpContext;
            var requestParser = new RequestParser();
            var requestParameters = requestParser.RequestParameters;
            var requestFactory = new HttpRequestFactory(requestParameters);
            var clientUrlBuilder = new Omikron.FactFinder.Core.Client.UrlBuilder(requestParser);
            var suggestAdapter = new Suggest(requestFactory.GetRequest(), clientUrlBuilder);
            var result = (from p in suggestAdapter.Suggestions
                          select new
                          {
                              label = p.Query,
                              producturl = p.Url,
                              productpictureurl = p.ImageUrl
                          })
                .ToList();
            return Json(result);
            //return Json(suggestAdapter.RawSuggestions.)
        }

        #endregion
    }
}