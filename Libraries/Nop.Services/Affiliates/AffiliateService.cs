using System;
using System.Linq;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;
using System.Collections.Generic;

namespace Nop.Services.Affiliates
{
    /// <summary>
    /// Affiliate service
    /// </summary>
    public partial class AffiliateService : IAffiliateService
    {
        #region Fields

        private readonly IRepository<Affiliate> _affiliateRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IEventPublisher _eventPublisher;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="affiliateRepository">Affiliate repository</param>
        /// <param name="orderRepository">Order repository</param>
        /// <param name="eventPublisher">Event published</param>
        public AffiliateService(IRepository<Affiliate> affiliateRepository,
            IRepository<Order> orderRepository,
            IEventPublisher eventPublisher)
        {
            this._affiliateRepository = affiliateRepository;
            this._orderRepository = orderRepository;
            this._eventPublisher = eventPublisher;
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Gets an affiliate by affiliate identifier
        /// </summary>
        /// <param name="affiliateId">Affiliate identifier</param>
        /// <returns>Affiliate</returns>
        public virtual Affiliate GetAffiliateById(int affiliateId)
        {
            if (affiliateId == 0)
                return null;
            
            return _affiliateRepository.GetById(affiliateId);


        }

        /// <summary>
        /// Gets an affiliate by affiliate GUID
        /// </summary>
        /// <param name="affiliateGuid">Affiliate GUID</param>
        /// <returns>Affiliate</returns>
        public virtual Affiliate GetAffiliateByGuid(Guid affiliateGuid)
        {                       
            if (affiliateGuid == Guid.Empty) return null;

            var affiliates = from ar in _affiliateRepository.Table
                                          where ar.Guid == affiliateGuid
                                          orderby ar.Id
                                          select ar;

            return affiliates.FirstOrDefault();
        }

        
        
        /// <summary>
        /// Gets an affiliate by friendly url name
        /// </summary>
        /// <param name="friendlyUrlName">Friendly url name</param>
        /// <returns>Affiliate</returns>
        public virtual Affiliate GetAffiliateByFriendlyUrlName(string friendlyUrlName)
        {
            if (String.IsNullOrWhiteSpace(friendlyUrlName))
                return null;

            var query = from a in _affiliateRepository.Table
                        orderby a.Id
                        where a.FriendlyUrlName == friendlyUrlName
                        select a;
            var affiliate = query.FirstOrDefault();
            return affiliate;
        }

        /// <summary>
        /// Marks affiliate as deleted 
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        public virtual void DeleteAffiliate(Affiliate affiliate)
        {
            if (affiliate == null)
                throw new ArgumentNullException("affiliate");
            _affiliateRepository.Delete(affiliate);            
        }

        /// <summary>
        /// Gets all affiliates
        /// </summary>
        /// <param name="friendlyUrlName">Friendly URL name; null to load all records</param>
        /// <param name="firstName">First name; null to load all records</param>
        /// <param name="email">Last name; null to load all records</param>        
        /// <param name="createdFromUtc">Orders created date from (UTC); null to load all records. It's used only with "loadOnlyWithOrders" parameter st to "true".</param>
        /// <param name="createdToUtc">Orders created date to (UTC); null to load all records. It's used only with "loadOnlyWithOrders" parameter st to "true".</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="onlyConfirmed">A value indicating whether to show hidden records</param>
        /// <returns>Affiliates</returns>
        public virtual IPagedList<Affiliate> GetAllAffiliates(string firstName = null, string email = null,            
            DateTime? createdFromUtc = null, DateTime?createdToUtc = null,int productId=0,
            int pageIndex = 0, int pageSize = int.MaxValue,
            bool onlyConfirmed = false, bool? isActive = null)
        {
            var query = _affiliateRepository.Table;            
            if (!String.IsNullOrWhiteSpace(firstName))
                query = query.Where(a => a.Address.FirstName.Contains(firstName));
            if (!String.IsNullOrWhiteSpace(email))
                query = query.Where(a => a.Address.Email.Contains(email));
            if (onlyConfirmed)
                query = query.Where(a => a.Deleted); 
            if(isActive.HasValue)
                query = query.Where(a => a.Active== isActive.Value);

            if (createdFromUtc.HasValue)
                query = query.Where(a => createdFromUtc.Value <= a.Address.CreatedOnUtc);
            if (createdToUtc.HasValue)
                query = query.Where(a => createdFromUtc.Value >= a.Address.CreatedOnUtc);
            if(productId>0)
                query = query.Where(a => a.AffiliateProducts.Select(p=>p.Id).Contains(productId));
            query = query.OrderByDescending(a => a.Id);

            var affiliates = new PagedList<Affiliate>(query, pageIndex, pageSize);
            return affiliates;
        }

        /// <summary>
        /// Inserts an affiliate
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        public virtual void InsertAffiliate(Affiliate affiliate)
        {
            if (affiliate == null)
                throw new ArgumentNullException("affiliate");

            _affiliateRepository.Insert(affiliate);

            //event notification
            _eventPublisher.EntityInserted(affiliate);
        }

        /// <summary>
        /// Updates the affiliate
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        public virtual void UpdateAffiliate(Affiliate affiliate)
        {
            if (affiliate == null)
                throw new ArgumentNullException("affiliate");

            _affiliateRepository.Update(affiliate);

            //event notification
            _eventPublisher.EntityUpdated(affiliate);
        }

        /// <summary>
        /// Get affiliate by identifiers
        /// </summary>
        /// <param name="affiliateIds">Affiliate identifiers</param>
        /// <returns>Order</returns>
        public virtual IList<Affiliate> GetAffiliatesByIds(int[] affiliateIds)
        {
            if (affiliateIds == null || affiliateIds.Length == 0)
                return new List<Affiliate>();

            var query = from a in _affiliateRepository.Table
                        where affiliateIds.Contains(a.Id)
                        select a;
            return query.ToList();
            
        }

        #endregion
    }
}