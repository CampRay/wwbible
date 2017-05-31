using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Nop.Admin.Extensions;
using Nop.Admin.Models.Messages;
using Nop.Core;
using Nop.Core.Domain.Messages;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Services.Affiliates;

namespace Nop.Admin.Controllers
{
	public partial class CampaignController : BaseAdminController
	{
        private readonly ICampaignService _campaignService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IEmailAccountService _emailAccountService;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly ILocalizationService _localizationService;
        private readonly IMessageTokenProvider _messageTokenProvider;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly IPermissionService _permissionService;
	    private readonly ICustomerService _customerService;
        private readonly IAffiliateService _affiliateService;

        public CampaignController(ICampaignService campaignService,
            IDateTimeHelper dateTimeHelper, 
            IEmailAccountService emailAccountService,
            EmailAccountSettings emailAccountSettings,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            ILocalizationService localizationService, 
            IMessageTokenProvider messageTokenProvider,
            IStoreContext storeContext,
            IStoreService storeService,
            IPermissionService permissionService, 
            ICustomerService customerService,
            IAffiliateService affiliateService)
		{
            this._campaignService = campaignService;
            this._dateTimeHelper = dateTimeHelper;
            this._emailAccountService = emailAccountService;
            this._emailAccountSettings = emailAccountSettings;
            this._newsLetterSubscriptionService = newsLetterSubscriptionService;
            this._localizationService = localizationService;
            this._messageTokenProvider = messageTokenProvider;
            this._storeContext = storeContext;
            this._storeService = storeService;
            this._permissionService = permissionService;
            this._customerService = customerService;
            this._affiliateService = affiliateService;

        }

        [NonAction]
        protected virtual string FormatTokens(string[] tokens)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                sb.Append(token);
                if (i != tokens.Length - 1)
                    sb.Append(", ");
            }

            return sb.ToString();
        }

        [NonAction]
        protected virtual void PrepareStoresModel(CampaignModel model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            model.AvailableStores.Add(new SelectListItem
            {
                Text = _localizationService.GetResource("Admin.Common.All"),
                Value = "0"
            });
            var stores = _storeService.GetAllStores();
            foreach (var store in stores)
            {
                model.AvailableStores.Add(new SelectListItem
                {
                    Text = store.Name,
                    Value = store.Id.ToString()
                });
            }
        }
        
        [NonAction]
        protected virtual void PrepareCustomerRolesModel(CampaignModel model)
	    {
            if (model == null)
                throw new ArgumentNullException("model");

            model.AvailableCustomerRoles.Add(new SelectListItem
            {
                Text = _localizationService.GetResource("Admin.Common.All"),
                Value = "0"
            });
            var roles = _customerService.GetAllCustomerRoles();
            foreach (var customerRole in roles)
            {
                model.AvailableCustomerRoles.Add(new SelectListItem
                {
                    Text = customerRole.Name,
                    Value = customerRole.Id.ToString()
                });
            }
        }

        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

		public ActionResult List()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCampaigns))
                return AccessDeniedView();

            //var stores = _storeService.GetAllStores();
            var model = new CampaignListModel();

            //model.AvailableStores.Add(new SelectListItem
            //{
            //    Text = _localizationService.GetResource("Admin.Common.All"),
            //    Value = "0"
            //});
            
            //foreach (var store in stores)
            //{
            //    model.AvailableStores.Add(new SelectListItem
            //    {
            //        Text = store.Name,
            //        Value = store.Id.ToString()
            //    });
            //}

            return View(model);
		}

        [HttpPost]
        public ActionResult List(DataSourceRequest command, CampaignListModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCampaigns))
                return AccessDeniedView();

            var campaigns = _campaignService.GetAllCampaigns();
            var gridModel = new DataSourceResult
            {
                Data = campaigns.Select(x =>
                {
                    var model = x.ToModel();
                    model.CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
                    if (x.DontSendBeforeDateUtc.HasValue)
                        model.DontSendBeforeDate = _dateTimeHelper.ConvertToUserTime(x.DontSendBeforeDateUtc.Value, DateTimeKind.Utc);
                    return model;
                }),
                Total = campaigns.Count
            };
            return Json(gridModel);
        }

        public ActionResult Create()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCampaigns))
                return AccessDeniedView();

            var model = new CampaignModel();
            model.AllowedTokens = FormatTokens(_messageTokenProvider.GetListOfCampaignAllowedTokens());
            ////stores
            //PrepareStoresModel(model);
            ////customer roles
            //PrepareCustomerRolesModel(model);

            //EmailType
            model.EmailType.Add(new SelectListItem
            {
                Value = "0",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.All")
            });
            model.EmailType.Add(new SelectListItem
            {
                Value = "1",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.Donation")
            });
            model.EmailType.Add(new SelectListItem
            {
                Value = "2",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.Lecture")
            });
            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public ActionResult Create(CampaignModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCampaigns))
                return AccessDeniedView();

            if (ModelState.IsValid)
            {
                var campaign = model.ToEntity();
                campaign.CustomerRoleId = model.EmailTypeId;
                campaign.CreatedOnUtc = DateTime.UtcNow;
                campaign.DontSendBeforeDateUtc = model.DontSendBeforeDate.HasValue ?
                    (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.DontSendBeforeDate.Value) : null;
                _campaignService.InsertCampaign(campaign);

                SuccessNotification(_localizationService.GetResource("Admin.Promotions.Campaigns.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = campaign.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            model.AllowedTokens = FormatTokens(_messageTokenProvider.GetListOfCampaignAllowedTokens());

            //EmailType
            model.EmailType.Add(new SelectListItem
            {
                Value = "0",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.All")
            });
            model.EmailType.Add(new SelectListItem
            {
                Value = "1",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.Donation")
            });
            model.EmailType.Add(new SelectListItem
            {
                Value = "2",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.Lecture")
            });
            ////stores
            //PrepareStoresModel(model);
            ////customer roles
            //PrepareCustomerRolesModel(model);
            return View(model);
        }

		public ActionResult Edit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCampaigns))
                return AccessDeniedView();

            var campaign = _campaignService.GetCampaignById(id);
            if (campaign == null)
                //No campaign found with the specified id
                return RedirectToAction("List");

            var model = campaign.ToModel();
            model.EmailTypeId = campaign.CustomerRoleId;
            if (campaign.DontSendBeforeDateUtc.HasValue)
                model.DontSendBeforeDate = _dateTimeHelper.ConvertToUserTime(campaign.DontSendBeforeDateUtc.Value, DateTimeKind.Utc);
            model.AllowedTokens = FormatTokens(_messageTokenProvider.GetListOfCampaignAllowedTokens());

            //EmailType
            model.EmailType.Add(new SelectListItem
            {
                Value = "0",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.All")
            });
            model.EmailType.Add(new SelectListItem
            {
                Value = "1",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.Donation")
            });
            model.EmailType.Add(new SelectListItem
            {
                Value = "2",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.Lecture")
            });

            ////stores
            //PrepareStoresModel(model);
            ////customer roles
            //PrepareCustomerRolesModel(model);
            return View(model);
		}

        [HttpPost]
        [ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        public ActionResult Edit(CampaignModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCampaigns))
                return AccessDeniedView();

            var campaign = _campaignService.GetCampaignById(model.Id);
            if (campaign == null)
                //No campaign found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                campaign = model.ToEntity(campaign);
                campaign.CustomerRoleId = model.EmailTypeId;
                campaign.DontSendBeforeDateUtc = model.DontSendBeforeDate.HasValue ?
                    (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.DontSendBeforeDate.Value) : null;
                _campaignService.UpdateCampaign(campaign);

                SuccessNotification(_localizationService.GetResource("Admin.Promotions.Campaigns.Updated"));
                return continueEditing ? RedirectToAction("Edit", new { id = campaign.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            model.AllowedTokens = FormatTokens(_messageTokenProvider.GetListOfCampaignAllowedTokens());

            //EmailType
            model.EmailType.Add(new SelectListItem
            {
                Value = "0",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.All")
            });
            model.EmailType.Add(new SelectListItem
            {
                Value = "1",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.Donation")
            });
            model.EmailType.Add(new SelectListItem
            {
                Value = "2",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.Lecture")
            });
            ////stores
            //PrepareStoresModel(model);
            ////customer roles
            //PrepareCustomerRolesModel(model);
            return View(model);
		}

        [HttpPost,ActionName("Edit")]
        [FormValueRequired("send-test-email")]
        public ActionResult SendTestEmail(CampaignModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCampaigns))
                return AccessDeniedView();

            var campaign = _campaignService.GetCampaignById(model.Id);
            if (campaign == null)
                //No campaign found with the specified id
                return RedirectToAction("List");
            
            model.AllowedTokens = FormatTokens(_messageTokenProvider.GetListOfCampaignAllowedTokens());

            //EmailType
            model.EmailType.Add(new SelectListItem
            {
                Value = "0",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.All")
            });
            model.EmailType.Add(new SelectListItem
            {
                Value = "1",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.Donation")
            });
            model.EmailType.Add(new SelectListItem
            {
                Value = "2",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.Lecture")
            });

            ////stores
            //PrepareStoresModel(model);
            ////customer roles
            //PrepareCustomerRolesModel(model);

            if (!CommonHelper.IsValidEmail(model.TestEmail))
            {
                ErrorNotification(_localizationService.GetResource("Admin.Common.WrongEmail"), false);
                return View(model);
            }

            try
            {
                var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
                if (emailAccount == null)
                    throw new NopException("Email account could not be loaded");


                //var subscription = _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(model.TestEmail, _storeContext.CurrentStore.Id);
                //if (subscription != null)
                //{
                //    //there's a subscription. let's use it
                //    var subscriptions = new List<NewsLetterSubscription>();
                //    subscriptions.Add(subscription);
                //    _campaignService.SendCampaign(campaign, emailAccount, subscriptions);
                //}
                //else
                //{
                //    //no subscription found
                //    _campaignService.SendCampaign(campaign, emailAccount, model.TestEmail);
                //}
                _campaignService.SendCampaign(campaign, emailAccount, model.TestEmail);
                SuccessNotification(_localizationService.GetResource("Admin.Promotions.Campaigns.TestEmailSentToCustomers"), false);
                return View(model);
            }
            catch (Exception exc)
            {
                ErrorNotification(exc, false);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("send-mass-email")]
        public ActionResult SendMassEmail(CampaignModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCampaigns))
                return AccessDeniedView();

            var campaign = _campaignService.GetCampaignById(model.Id);
            if (campaign == null)
                //No campaign found with the specified id
                return RedirectToAction("List");

            model.AllowedTokens = FormatTokens(_messageTokenProvider.GetListOfCampaignAllowedTokens());

            //EmailType
            model.EmailType.Add(new SelectListItem
            {
                Value = "0",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.All")
            });
            model.EmailType.Add(new SelectListItem
            {
                Value = "1",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.Donation")
            });
            model.EmailType.Add(new SelectListItem
            {
                Value = "2",
                Text = _localizationService.GetResource("Admin.Promotions.Campaign.List.EmailType.Lecture")
            });
            ////stores
            //PrepareStoresModel(model);
            ////customer roles
            //PrepareCustomerRolesModel(model);

            try
            {
                var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
                if (emailAccount == null)
                    throw new NopException("Email account could not be loaded");

                //subscribers of certain store?
                //var store = _storeService.GetStoreById(campaign.StoreId);
                //var storeId = store != null ? store.Id : 0;
                IList<string> emailList = new List<string>();
                if (model.EmailTypeId == 0)
                {
                    var subscriptions = _newsLetterSubscriptionService.GetAllNewsLetterSubscriptions(isActive: true);
                    var affiliates=_affiliateService.GetAllAffiliates();
                    foreach (var item in subscriptions)
                    {
                        emailList.Add(item.Email);
                    }
                    foreach (var item in affiliates)
                    {
                        emailList.Add(item.Address.Email);
                    }
                }
                else if(model.EmailTypeId == 1)
                {
                    var subscriptions = _newsLetterSubscriptionService.GetAllNewsLetterSubscriptions(isActive: true);
                    foreach (var item in subscriptions)
                    {
                        emailList.Add(item.Email);
                    }
                }
                else if (model.EmailTypeId == 2)
                {
                    if (model.LectureId > 0) {
                        var affiliates = _affiliateService.GetAllAffiliates(isActive: true, productId: model.LectureId);
                        foreach (var item in affiliates)
                        {
                            emailList.Add(item.Address.Email);
                        }
                    }
                    else
                    {
                        var affiliates = _affiliateService.GetAllAffiliates(isActive: true);
                        foreach (var item in affiliates)
                        {
                            emailList.Add(item.Address.Email);
                        }
                    }                                        
                }
                
                // var subscriptions = _newsLetterSubscriptionService.GetAllNewsLetterSubscriptions(isActive: true);
                var totalEmailsSent = _campaignService.SendCampaign(campaign, emailAccount, emailList.Distinct().ToList());
                SuccessNotification(string.Format(_localizationService.GetResource("Admin.Promotions.Campaigns.MassEmailSentToCustomers"), totalEmailsSent), false);
                return View(model);
            }
            catch (Exception exc)
            {
                ErrorNotification(exc, false);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

		[HttpPost]
        public ActionResult Delete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCampaigns))
                return AccessDeniedView();

            var campaign = _campaignService.GetCampaignById(id);
            if (campaign == null)
                //No campaign found with the specified id
                return RedirectToAction("List");

            _campaignService.DeleteCampaign(campaign);

            SuccessNotification(_localizationService.GetResource("Admin.Promotions.Campaigns.Deleted"));
			return RedirectToAction("List");
		}
	}
}
