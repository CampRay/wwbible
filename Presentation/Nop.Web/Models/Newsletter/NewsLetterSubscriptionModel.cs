using System;
using System.Web.Mvc;
using FluentValidation.Attributes;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Nop.Web.Validators.Newsletter;

namespace Nop.Web.Models.Newsletter
{
    [Validator(typeof(NewsLetterSubscriptionValidator))]
    public partial class NewsLetterSubscriptionModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Email")]
        [AllowHtml]
        public string Email { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Active")]
        [AllowHtml]
        public bool Active { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Store")]
        public string StoreName { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Participation")]
        [AllowHtml]
        public bool Participation { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Monthly")]
        [AllowHtml]
        public bool Monthly { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Amount")]
        [AllowHtml]
        public string Amount { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Cname")]
        [AllowHtml]
        public string Cname { get; set; }        

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Ename")]
        [AllowHtml]
        public string Ename { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Gender")]
        [AllowHtml]
        public bool Gender { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Phone")]
        [AllowHtml]
        public string Phone { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Address")]
        [AllowHtml]
        public string Address { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Church")]
        [AllowHtml]
        public string Church { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.MailCheck")]
        [AllowHtml]
        public bool MailCheck { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Deposit")]
        [AllowHtml]
        public bool Deposit { get; set; }

        [NopResourceDisplayName("NewsLetterSubscriptions.Fields.Virement")]
        [AllowHtml]
        public bool Virement { get; set; }

        public bool Successfully { get; set; }
        public string Result { get; set; }
    }
}