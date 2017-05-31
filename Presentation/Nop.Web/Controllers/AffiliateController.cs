using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Models.Affiliate;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Services.Common;
using Nop.Services.Messages;
using Nop.Web.Models.Newsletter;

namespace Nop.Web.Controllers
{
    public partial class AffiliateController : BaseController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IWebHelper _webHelper;
        private readonly IAffiliateService _affiliateService;
        private readonly IProductService _productService;
        private readonly IAddressService _addressService;
        private readonly IWorkflowMessageService _workflowMessageService;

        #endregion

        #region Constructors

        public AffiliateController(ILocalizationService localizationService,
            IWorkContext workContext, IDateTimeHelper dateTimeHelper, IWebHelper webHelper,
             IAffiliateService affiliateService, IProductService productService, IAddressService addressService,
             IWorkflowMessageService workflowMessageService)
        {
            this._localizationService = localizationService;
            this._workContext = workContext;
            this._dateTimeHelper = dateTimeHelper;
            this._webHelper = webHelper;
            this._affiliateService = affiliateService;
            this._productService = productService;
            this._addressService = addressService;
            this._workflowMessageService = workflowMessageService;
        }

        #endregion        

        #region Methods

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(AffiliateModel model)
        {
            var address = new Address();
            address.FirstName = model.Name;
            address.CreatedOnUtc = DateTime.UtcNow;
            address.Email = model.Email;
            address.PhoneNumber = model.Phone;
            address.Company = model.Church;
           // _addressService.InsertAddress(address);
            var affiliate = new Affiliate();
            ICollection<Product> affiliateProducts = new List<Product>();
            affiliate.Active = model.Active;
            affiliate.Guid = Guid.NewGuid();
            affiliate.Deleted = false;
            affiliate.AdminComment = "";
            affiliate.FriendlyUrlName = "";            
            affiliate.Address = address;
            
            foreach (var item in model.ProductIds)
            {
                var product = _productService.GetProductById(item);                
                affiliateProducts.Add(product);
            }

            affiliate.AffiliateProducts = affiliateProducts;
            _affiliateService.InsertAffiliate(affiliate);
            _workflowMessageService.SendApplyLectureActivationMessage(affiliate, _workContext.WorkingLanguage.Id);
            return Json(new
            {
                Success = true
            });

        }

        public ActionResult Activation(Guid token)
        {
            var affiliate = _affiliateService.GetAffiliateByGuid(token);
            if (affiliate == null)
                return RedirectToRoute("HomePage");

            var model = new SubscriptionActivationModel();

            affiliate.Deleted = true;
            _affiliateService.UpdateAffiliate(affiliate);

            model.Result = _localizationService.GetResource("JoinLectures.ResultActivated");

            return View(model);
        }

        #endregion
    }
}
