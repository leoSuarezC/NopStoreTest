﻿using Nop.Core.Domain.Forums;

namespace Nop.Data.Mapping.Forums
{
    /// <summary>
    /// Mapping class
    /// </summary>
    public partial class PrivateMessageMap : NopEntityTypeConfiguration<PrivateMessage>
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public PrivateMessageMap()
        {
            this.ToTable("Forums_PrivateMessage");
            this.HasKey(pm => pm.Id);
            this.Property(pm => pm.Subject).IsRequired().HasMaxLength(450);
            this.Property(pm => pm.Text).IsRequired();

            this.HasRequired(pm => pm.FromCustomer)
               .WithMany()
               .HasForeignKey(pm => pm.FromCustomerId)
               .WillCascadeOnDelete(false);

            this.HasRequired(pm => pm.ToCustomer)
               .WithMany()
               .HasForeignKey(pm => pm.ToCustomerId)
               .WillCascadeOnDelete(false);
        }
    }
}
