using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;

namespace Nop.Web.Components
{
    public class HomepageCategoriesViewComponent : NopViewComponent
    {
        private readonly ICatalogModelFactory _catalogModelFactory;
        private readonly ICustomStoreService _customStoreService;

        public HomepageCategoriesViewComponent(ICatalogModelFactory catalogModelFactory,
            ICustomStoreService customStoreService)
        {
            this._catalogModelFactory = catalogModelFactory;
            this._customStoreService = customStoreService;
        }

        public IViewComponentResult Invoke()
        {
            ViewBag.CategorySeoMap = _customStoreService.GetCategorySeoMapping();

            var model = _catalogModelFactory.PrepareHomepageCategoryModels();
            if (!model.Any())
                return Content("");

            return View(model);
        }
    }
}
