using Nop.Core.Domain.Messages;

namespace Nop.Data.Mapping.Messages
{
    public partial class NewsLetterSubscriptionMap : NopEntityTypeConfiguration<NewsLetterSubscription>
    {
        public NewsLetterSubscriptionMap()
        {
            this.ToTable("NewsLetterSubscription");
            this.HasKey(nls => nls.Id);

            this.Property(nls => nls.Email).IsRequired().HasMaxLength(255);
            this.Property(nls => nls.Amount).IsRequired().HasMaxLength(100);
            this.Property(nls => nls.Cname).IsRequired().HasMaxLength(100);
            this.Property(nls => nls.Phone).IsRequired().HasMaxLength(100);
            this.Property(nls => nls.Address).IsRequired().HasMaxLength(100);
        }
    }
}