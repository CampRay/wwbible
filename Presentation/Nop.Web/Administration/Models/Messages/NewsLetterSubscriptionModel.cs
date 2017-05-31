using System;
using System.Web.Mvc;
using FluentValidation.Attributes;
using Nop.Admin.Validators.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Messages
{
    [Validator(typeof(NewsLetterSubscriptionValidator))]
    public partial class NewsLetterSubscriptionModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Admin.Promotions.NewsLetterSubscriptions.Fields.Email")]
        [AllowHtml]
        public string Email { get; set; }

        [NopResourceDisplayName("Admin.Promotions.NewsLetterSubscriptions.Fields.Active")]
        public bool Active { get; set; }

        [NopResourceDisplayName("Admin.Promotions.NewsLetterSubscriptions.Fields.Store")]
        public string StoreName { get; set; }

        [NopResourceDisplayName("Admin.Promotions.NewsLetterSubscriptions.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        public bool Participation { get; set; }
        public bool Monthly { get; set; }
        public string Amount { get; set; }
        public string Cname { get; set; }
        public string Ename { get; set; }
        public bool Gender { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Church { get; set; }
        public bool MailCheck { get; set; }
        public bool Deposit { get; set; }
        public bool Virement { get; set; }
    }
}