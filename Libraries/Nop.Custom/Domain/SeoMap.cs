using Nop.Core;

namespace Nop.Custom.Domain
{
    public partial class SeoMap : BaseEntity
    {
        public virtual string text_orig { get; set; }
        public virtual string text_seo { get; set; }
    }

}
