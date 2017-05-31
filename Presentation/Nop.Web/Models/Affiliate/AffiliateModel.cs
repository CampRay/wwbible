using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Affiliate
{
    public partial class AffiliateModel : BaseNopModel
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Church { get; set; }
        public bool Active { get; set; }
        public int[] ProductIds { get; set; }
    }
}