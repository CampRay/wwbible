using FluentValidation;
using Nop.Core.Domain.Messages;
using Nop.Data;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;
using Nop.Web.Models.Newsletter;

namespace Nop.Web.Validators.Newsletter
{
    public partial class NewsLetterSubscriptionValidator : BaseNopValidator<NewsLetterSubscriptionModel>
    {
        public NewsLetterSubscriptionValidator(ILocalizationService localizationService, IDbContext dbContext)
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage(localizationService.GetResource("NewsLetterSubscriptions.Fields.Email.Required"));
            RuleFor(x => x.Email).EmailAddress().WithMessage(localizationService.GetResource("Common.WrongEmail"));
            RuleFor(x => x.Cname).NotEmpty().WithMessage(localizationService.GetResource("NewsLetterSubscriptions.Fields.Cname.Required"));
            RuleFor(x => x.Amount).NotEmpty().WithMessage(localizationService.GetResource("NewsLetterSubscriptions.Fields.Amount.Required"));
            RuleFor(x => x.Phone).NotEmpty().WithMessage(localizationService.GetResource("NewsLetterSubscriptions.Fields.Phone.Required"));
            RuleFor(x => x.Address).NotEmpty().WithMessage(localizationService.GetResource("NewsLetterSubscriptions.Fields.Address.Required"));            

            SetStringPropertiesMaxLength<NewsLetterSubscription>(dbContext);
        }
    }
}