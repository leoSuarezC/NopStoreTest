using Nop.Core;

namespace Nop.Custom.Domain
{
    public partial class SeoInfoModel
    {
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string H1 { get; set; }

        public string SeoText1 { get; set; }
        public string SeoText2 { get; set; }

        public string CanonicalTag { get; set; }
        public bool Index { get; set; }
        public bool Follow { get; set; }

        public string PreviousPage { get; set; }
        public string NextPage { get; set; }

        //public IList<AlternativeCategory> AlternativeCategories { get; set; }

        public SeoInfoModel()
        {
            MetaTitle = "";
            MetaDescription = "";
            H1 = "";
            SeoText1 = "";
            SeoText2 = "";
            CanonicalTag = "";
            PreviousPage = "";
            NextPage = "";
        }
    }

}
