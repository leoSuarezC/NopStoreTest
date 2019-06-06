using Nop.Core;

namespace Nop.Custom.Domain
{
    /// <summary>
    /// Represents a product category mapping
    /// </summary>
    public partial class MPay24PaymentOptions : BaseEntity
    {
        /// <summary>
        /// Gets or sets the ProductVariantId identifier
        /// </summary>
        public virtual string PaymentType{ get; set; }
        public virtual string Brand { get; set; }
        public virtual string DisplayName { get; set; }
        public virtual string Logo { get; set; }
        public virtual string Description { get; set; }  
    }

}
