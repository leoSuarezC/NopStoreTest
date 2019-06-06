using System.Collections.Generic;
using System.Collections.Specialized;
using Nop.Core.Domain.Stores;
using Nop.Custom.Domain;

namespace Nop.Services.Stores
{
    /// <summary>
    /// Store service interface
    /// </summary>
    public partial interface ICustomStoreService
    {
        /// <summary>
        /// Get all MPay24 PaymentOptions
        /// </summary>
        /// <param name="store">Store</param>
        IList<MPay24PaymentOptions> GetAllMPay24PaymentOptions();
        IList<MPay24PaymentOptions> GetSelectedMPay24PaymentOptions();
        IList<SeoMap> GetCategorySeoMapping();
        IList<SeoMap> GetManufacturerSeoMapping();
        SeoInfoModel GetSeoInfo(NameValueCollection sp, IList<SeoMap> categorySeoMap);
    }
}