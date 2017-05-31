using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Affiliates
{
    public partial class AffiliateListModel : BaseNopModel
    {
        [NopResourceDisplayName("Admin.Affiliates.List.SearchFirstName")]
        [AllowHtml]
        public string SearchFirstName { get; set; }

        [NopResourceDisplayName("Admin.Affiliates.List.SearchEmail")]
        [AllowHtml]
        public string SearchEmail { get; set; }

        //[NopResourceDisplayName("Admin.Affiliates.List.SearchFriendlyUrlName")]
        //[AllowHtml]
        //public string SearchFriendlyUrlName { get; set; }

        //[NopResourceDisplayName("Admin.Affiliates.List.LoadOnlyWithOrders")]
        //public bool LoadOnlyWithOrders { get; set; }
        [NopResourceDisplayName("Admin.Affiliates.List.OrdersCreatedFromUtc")]
        [UIHint("DateNullable")]
        public DateTime? CreatedFromUtc { get; set; }
        [NopResourceDisplayName("Admin.Affiliates.List.OrdersCreatedToUtc")]
        [UIHint("DateNullable")]
        public DateTime? CreatedToUtc { get; set; }

        [NopResourceDisplayName("Admin.Affiliates.List.OnlyConfirmed")]
        public bool OnlyConfirmed { get; set; }

        [NopResourceDisplayName("Admin.Affiliates.List.SearchLectureId")]        
        public int SearchLectureId { get; set; }
    }
}