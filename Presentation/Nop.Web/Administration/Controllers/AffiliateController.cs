using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Admin.Extensions;
using Nop.Admin.Models.Affiliates;
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
using Nop.Core.Domain.Catalog;
using Nop.Services.ExportImport;
using Nop.Core.Domain.Common;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Controllers
{
    public partial class AffiliateController : BaseAdminController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IWebHelper _webHelper;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IAffiliateService _affiliateService;
        private readonly ICustomerService _customerService;
        private readonly IOrderService _orderService;
        private readonly IPermissionService _permissionService;
        private readonly IExportManager _exportManager;

        #endregion

        #region Constructors

        public AffiliateController(ILocalizationService localizationService,
            IWorkContext workContext, IDateTimeHelper dateTimeHelper, IWebHelper webHelper,
            ICountryService countryService, IStateProvinceService stateProvinceService,
            IPriceFormatter priceFormatter, IAffiliateService affiliateService,
            ICustomerService customerService, IOrderService orderService,
            IPermissionService permissionService, IExportManager exportManager)
        {
            this._localizationService = localizationService;
            this._workContext = workContext;
            this._dateTimeHelper = dateTimeHelper;
            this._webHelper = webHelper;
            this._countryService = countryService;
            this._stateProvinceService = stateProvinceService;
            this._priceFormatter = priceFormatter;
            this._affiliateService = affiliateService;
            this._customerService = customerService;
            this._orderService = orderService;
            this._permissionService = permissionService;
            this._exportManager = exportManager;
        }

        #endregion

        #region Utilities

        [NonAction]
        protected virtual void PrepareAffiliateModel(AffiliateModel model, Affiliate affiliate, bool excludeProperties,
            bool prepareEntireAddressModel = true)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (affiliate != null)
            {
                model.Id = affiliate.Id;
                model.Url = affiliate.GenerateUrl(_webHelper);
                if (!excludeProperties)
                {
                    model.AdminComment = affiliate.AdminComment;
                    model.FriendlyUrlName = affiliate.FriendlyUrlName;
                    model.Active = affiliate.Active;
                    model.Deleted = affiliate.Deleted;
                    //model.Address = affiliate.Address.ToModel();
                    model.Name = affiliate.Address.FirstName;
                    model.Phone = affiliate.Address.PhoneNumber;
                    model.Email = affiliate.Address.Email;
                    model.Church = affiliate.Address.Company;
                    model.CreatedOn= _dateTimeHelper.ConvertToUserTime(affiliate.Address.CreatedOnUtc, DateTimeKind.Utc); 
                }
            }

            //if (prepareEntireAddressModel)
            //{
            //    model.Address.FirstNameEnabled = true;
            //    model.Address.FirstNameRequired = true;
            //    model.Address.LastNameEnabled = true;
            //    model.Address.LastNameRequired = true;
            //    model.Address.EmailEnabled = true;
            //    model.Address.EmailRequired = true;
            //    model.Address.CompanyEnabled = true;
            //    model.Address.CountryEnabled = true;
            //    model.Address.StateProvinceEnabled = true;
            //    model.Address.CityEnabled = true;
            //    model.Address.CityRequired = true;
            //    model.Address.StreetAddressEnabled = true;
            //    model.Address.StreetAddressRequired = true;
            //    model.Address.StreetAddress2Enabled = true;
            //    model.Address.ZipPostalCodeEnabled = true;
            //    model.Address.ZipPostalCodeRequired = true;
            //    model.Address.PhoneEnabled = true;
            //    model.Address.PhoneRequired = true;
            //    model.Address.FaxEnabled = true;

            //    //address
            //    model.Address.AvailableCountries.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Address.SelectCountry"), Value = "0" });
            //    foreach (var c in _countryService.GetAllCountries(showHidden: true))
            //        model.Address.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = (affiliate != null && c.Id == affiliate.Address.CountryId) });

            //    var states = model.Address.CountryId.HasValue ? _stateProvinceService.GetStateProvincesByCountryId(model.Address.CountryId.Value, showHidden: true).ToList() : new List<StateProvince>();
            //    if (states.Any())
            //    {
            //        foreach (var s in states)
            //            model.Address.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString(), Selected = (affiliate != null && s.Id == affiliate.Address.StateProvinceId) });
            //    }
            //    else
            //        model.Address.AvailableStates.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Address.OtherNonUS"), Value = "0" });
            //}
        }
        
        #endregion

        #region Methods

        //list
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        public ActionResult List()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            var model = new AffiliateListModel();
            return View(model);
        }

        [HttpPost]
        public ActionResult AffiliateList(DataSourceRequest command, AffiliateListModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            DateTime? startDateValue = (model.CreatedFromUtc == null) ? null
                            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.CreatedFromUtc.Value, _dateTimeHelper.CurrentTimeZone);

            DateTime? endDateValue = (model.CreatedToUtc == null) ? null
                            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.CreatedToUtc.Value, _dateTimeHelper.CurrentTimeZone).AddDays(1);

            var affiliates = _affiliateService.GetAllAffiliates(model.SearchFirstName, model.SearchEmail,
                 startDateValue, endDateValue, model.SearchLectureId,
                command.Page - 1, command.PageSize, model.OnlyConfirmed);

            var gridModel = new DataSourceResult
            {
                Data = affiliates.Select(x =>
                {
                    var m = new AffiliateModel();
                    PrepareAffiliateModel(m, x, false, false);
                    return m;
                }),
                Total = affiliates.TotalCount,
            };
            return Json(gridModel);
        }

        [HttpPost]
        public ActionResult AffiliateUpdate([Bind(Exclude = "CreatedOn")]AffiliateModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            if (!ModelState.IsValid)
            {
                return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });
            }

            var affiliate = _affiliateService.GetAffiliateById(model.Id);

            var address = new Address();
            affiliate.Address.FirstName = model.Name;
            affiliate.Address.Email = model.Email;
            affiliate.Address.PhoneNumber = model.Phone;
            affiliate.Address.Company = model.Church;            
            affiliate.Active = model.Active;            
            affiliate.Deleted = model.Deleted;            
                        
            _affiliateService.UpdateAffiliate(affiliate);                        

            return new NullJsonResult();
        }

        [HttpPost]
        public ActionResult AffiliateDelete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            var affiliate = _affiliateService.GetAffiliateById(id);
            if (affiliate == null)
                throw new ArgumentException("No lecture requestion found with the specified id");
            _affiliateService.DeleteAffiliate(affiliate);

            return new NullJsonResult();
        }



        ////create
        //public ActionResult Create()
        //{
        //    if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
        //        return AccessDeniedView();

        //    var model = new AffiliateModel();
        //    PrepareAffiliateModel(model, null, false);
        //    return View(model);
        //}

        //[HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        //[FormValueRequired("save", "save-continue")]
        //public ActionResult Create(AffiliateModel model, bool continueEditing)
        //{
        //    if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
        //        return AccessDeniedView();

        //    if (ModelState.IsValid)
        //    {
        //        var affiliate = new Affiliate();

        //        affiliate.Active = model.Active;
        //        affiliate.AdminComment = model.AdminComment;
        //        //validate friendly URL name
        //        var friendlyUrlName = affiliate.ValidateFriendlyUrlName(model.FriendlyUrlName);
        //        affiliate.FriendlyUrlName = friendlyUrlName;
        //        affiliate.Address = model.Address.ToEntity();
        //        affiliate.Address.CreatedOnUtc = DateTime.UtcNow;
        //        //some validation
        //        if (affiliate.Address.CountryId == 0)
        //            affiliate.Address.CountryId = null;
        //        if (affiliate.Address.StateProvinceId == 0)
        //            affiliate.Address.StateProvinceId = null;
        //        _affiliateService.InsertAffiliate(affiliate);

        //        SuccessNotification(_localizationService.GetResource("Admin.Affiliates.Added"));
        //        return continueEditing ? RedirectToAction("Edit", new { id = affiliate.Id }) : RedirectToAction("List");
        //    }

        //    //If we got this far, something failed, redisplay form
        //    PrepareAffiliateModel(model, null, true);
        //    return View(model);

        //}
        
        public ActionResult View(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            this.ViewData["AffiliateId"] = id;
            return View();            
        }

        [HttpPost]
        public ActionResult LecturePopupList(DataSourceRequest command, int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            var affiliate = _affiliateService.GetAffiliateById(id);                        
            var gridModel = new DataSourceResult();
            if (affiliate == null)
            {
                gridModel.Data = new List<Product>();
                gridModel.Total = 0;
                return Json(gridModel);
            }

            var products = new PagedList<Product>(affiliate.AffiliateProducts, command.Page - 1, command.PageSize);
            
            gridModel.Data = products.Select(x => x.ToModel());
            gridModel.Total = products.TotalCount;

            return Json(gridModel);
        }


        //edit
        public ActionResult Edit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
                return AccessDeniedView();

            var affiliate = _affiliateService.GetAffiliateById(id);
            if (affiliate == null || affiliate.Deleted)
                //No affiliate found with the specified id
                return RedirectToAction("List");

            var model = new AffiliateModel();
            PrepareAffiliateModel(model, affiliate, false);
            return View(model);
        }

        //[HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        //public ActionResult Edit(AffiliateModel model, bool continueEditing)
        //{
        //    if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
        //        return AccessDeniedView();

        //    var affiliate = _affiliateService.GetAffiliateById(model.Id);
        //    if (affiliate == null || affiliate.Deleted)
        //        //No affiliate found with the specified id
        //        return RedirectToAction("List");

        //    if (ModelState.IsValid)
        //    {
        //        affiliate.Active = model.Active;
        //        affiliate.AdminComment = model.AdminComment;
        //        //validate friendly URL name
        //        var friendlyUrlName = affiliate.ValidateFriendlyUrlName(model.FriendlyUrlName);
        //        affiliate.FriendlyUrlName = friendlyUrlName;
        //        affiliate.Address = model.Address.ToEntity(affiliate.Address);
        //        //some validation
        //        if (affiliate.Address.CountryId == 0)
        //            affiliate.Address.CountryId = null;
        //        if (affiliate.Address.StateProvinceId == 0)
        //            affiliate.Address.StateProvinceId = null;
        //        _affiliateService.UpdateAffiliate(affiliate);

        //        SuccessNotification(_localizationService.GetResource("Admin.Affiliates.Updated"));
        //        if (continueEditing)
        //        {
        //            //selected tab
        //            SaveSelectedTabName();

        //            return RedirectToAction("Edit", new {id = affiliate.Id});
        //        }
        //        return RedirectToAction("List");
        //    }

        //    //If we got this far, something failed, redisplay form
        //    PrepareAffiliateModel(model, affiliate, true);
        //    return View(model);
        //}

        ////delete
        //[HttpPost]
        //public ActionResult Delete(int id)
        //{
        //    if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
        //        return AccessDeniedView();

        //    var affiliate = _affiliateService.GetAffiliateById(id);
        //    if (affiliate == null)
        //        //No affiliate found with the specified id
        //        return RedirectToAction("List");

        //    _affiliateService.DeleteAffiliate(affiliate);
        //    SuccessNotification(_localizationService.GetResource("Admin.Affiliates.Deleted"));
        //    return RedirectToAction("List");
        //}

        //[ChildActionOnly]
        //public ActionResult AffiliatedOrderList(int affiliateId)
        //{
        //    if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
        //        return Content("");

        //    if (affiliateId == 0)
        //        throw new Exception("Affliate ID cannot be 0");

        //    var model = new AffiliatedOrderListModel();
        //    model.AffliateId = affiliateId;

        //    //order statuses
        //    model.AvailableOrderStatuses = OrderStatus.Pending.ToSelectList(false).ToList();
        //    model.AvailableOrderStatuses.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });

        //    //payment statuses
        //    model.AvailablePaymentStatuses = PaymentStatus.Pending.ToSelectList(false).ToList();
        //    model.AvailablePaymentStatuses.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });

        //    //shipping statuses
        //    model.AvailableShippingStatuses = ShippingStatus.NotYetShipped.ToSelectList(false).ToList();
        //    model.AvailableShippingStatuses.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });

        //    return PartialView(model);
        //}
        //[HttpPost]
        //public ActionResult AffiliatedOrderListGrid(DataSourceRequest command, AffiliatedOrderListModel model)
        //{
        //    if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
        //        return AccessDeniedView();

        //    var affiliate = _affiliateService.GetAffiliateById(model.AffliateId);
        //    if (affiliate == null)
        //        throw new ArgumentException("No affiliate found with the specified id");

        //    DateTime? startDateValue = (model.StartDate == null) ? null
        //                    : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.StartDate.Value, _dateTimeHelper.CurrentTimeZone);

        //    DateTime? endDateValue = (model.EndDate == null) ? null
        //                    : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.EndDate.Value, _dateTimeHelper.CurrentTimeZone).AddDays(1);

        //    var orderStatusIds = model.OrderStatusId > 0 ? new List<int>() { model.OrderStatusId } : null;
        //    var paymentStatusIds = model.PaymentStatusId > 0 ? new List<int>() { model.PaymentStatusId } : null;
        //    var shippingStatusIds = model.ShippingStatusId > 0 ? new List<int>() { model.ShippingStatusId } : null;

        //    var orders = _orderService.SearchOrders(
        //        createdFromUtc: startDateValue,
        //        createdToUtc: endDateValue,
        //        osIds: orderStatusIds,
        //        psIds: paymentStatusIds,
        //        ssIds: shippingStatusIds,
        //        affiliateId: affiliate.Id,
        //        pageIndex: command.Page - 1,
        //        pageSize: command.PageSize);
        //    var gridModel = new DataSourceResult
        //    {
        //        Data = orders.Select(order =>
        //            {
        //                var orderModel = new AffiliateModel.AffiliatedOrderModel();
        //                orderModel.Id = order.Id;
        //                orderModel.OrderStatus = order.OrderStatus.GetLocalizedEnum(_localizationService, _workContext);
        //                orderModel.OrderStatusId = order.OrderStatusId;
        //                orderModel.PaymentStatus = order.PaymentStatus.GetLocalizedEnum(_localizationService, _workContext);
        //                orderModel.ShippingStatus = order.ShippingStatus.GetLocalizedEnum(_localizationService, _workContext);
        //                orderModel.OrderTotal = _priceFormatter.FormatPrice(order.OrderTotal, true, false);
        //                orderModel.CreatedOn = _dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc);
        //                return orderModel;
        //            }),
        //        Total = orders.TotalCount
        //    };

        //    return Json(gridModel);
        //}


        //[HttpPost]
        //public ActionResult AffiliatedCustomerList(int affiliateId, DataSourceRequest command)
        //{
        //    if (!_permissionService.Authorize(StandardPermissionProvider.ManageAffiliates))
        //        return AccessDeniedView();

        //    var affiliate = _affiliateService.GetAffiliateById(affiliateId);
        //    if (affiliate == null)
        //        throw new ArgumentException("No affiliate found with the specified id");

        //    var customers = _customerService.GetAllCustomers(
        //        affiliateId: affiliate.Id,
        //        pageIndex: command.Page - 1,
        //        pageSize: command.PageSize);
        //    var gridModel = new DataSourceResult
        //    {
        //        Data = customers.Select(customer =>
        //            {
        //                var customerModel = new AffiliateModel.AffiliatedCustomerModel();
        //                customerModel.Id = customer.Id;
        //                customerModel.Name = customer.Email;
        //                return customerModel;
        //            }),
        //        Total = customers.TotalCount
        //    };

        //    return Json(gridModel);
        //}

        [HttpPost, ActionName("List")]
        [FormValueRequired("exportexcel-all")]
        public ActionResult ExportExcelAll(AffiliateListModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            DateTime? startDateValue = (model.CreatedFromUtc == null) ? null
                            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.CreatedFromUtc.Value, _dateTimeHelper.CurrentTimeZone);

            DateTime? endDateValue = (model.CreatedToUtc == null) ? null
                            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.CreatedToUtc.Value, _dateTimeHelper.CurrentTimeZone).AddDays(1);

            var affiliates = _affiliateService.GetAllAffiliates(model.SearchFirstName, model.SearchEmail, startDateValue, endDateValue, model.SearchLectureId,
                0, int.MaxValue, model.OnlyConfirmed);

            try
            {
                byte[] bytes = _exportManager.ExportAffiliatesToXlsx(affiliates);
                return File(bytes, MimeTypes.TextXlsx, "JoinedUsers.xlsx");
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
                return RedirectToAction("List");
            }
        }

        [HttpPost]
        public ActionResult ExportExcelSelected(string selectedIds)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            //a vendor cannot export orders
            if (_workContext.CurrentVendor != null)
                return AccessDeniedView();

            var affiliates = new List<Affiliate>();
            if (selectedIds != null)
            {
                var ids = selectedIds
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => Convert.ToInt32(x))
                    .ToArray();
                affiliates.AddRange(_affiliateService.GetAffiliatesByIds(ids));
            }

            try
            {
                byte[] bytes = _exportManager.ExportAffiliatesToXlsx(affiliates);
                return File(bytes, MimeTypes.TextXlsx, "JoinedUsers.xlsx");
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
                return RedirectToAction("List");
            }
        }


        #endregion
    }
}
