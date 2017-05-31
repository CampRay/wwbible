using Nop.Core.Domain.Affiliates;

namespace Nop.Data.Mapping.Affiliates
{
    public partial class AffiliateMap : NopEntityTypeConfiguration<Affiliate>
    {
        public AffiliateMap()
        {
            this.ToTable("Affiliate");
            this.HasKey(a => a.Id);
            this.HasRequired(a => a.Address).WithMany().HasForeignKey(x => x.AddressId).WillCascadeOnDelete(false);
            this.HasMany(c => c.AffiliateProducts)
                .WithMany()
                .Map(m => m.ToTable("Affiliate_Product_Mapping"));
        }
    }
}