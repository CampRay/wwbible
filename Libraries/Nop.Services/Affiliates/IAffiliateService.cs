using System;
using Nop.Core;
using Nop.Core.Domain.Affiliates;
using System.Collections.Generic;

namespace Nop.Services.Affiliates
{
    /// <summary>
    /// Affiliate service interface
    /// </summary>
    public partial interface IAffiliateService
    {
        /// <summary>
        /// Gets an affiliate by affiliate identifier
        /// </summary>
        /// <param name="affiliateId">Affiliate identifier</param>
        /// <returns>Affiliate</returns>
        Affiliate GetAffiliateById(int affiliateId);

        /// <summary>
        /// Gets an affiliate by affiliate GUID
        /// </summary>
        /// <param name="affiliateGuid">Affiliate GUID</param>
        /// <returns>Affiliate</returns>
        Affiliate GetAffiliateByGuid(Guid affiliateGuid);

        /// <summary>
        /// Gets an affiliate by friendly url name
        /// </summary>
        /// <param name="friendlyUrlName">Friendly url name</param>
        /// <returns>Affiliate</returns>
        Affiliate GetAffiliateByFriendlyUrlName(string friendlyUrlName);

        /// <summary>
        /// Marks affiliate as deleted 
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        void DeleteAffiliate(Affiliate affiliate);

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
        IPagedList<Affiliate> GetAllAffiliates(string firstName = null, string email = null,
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null, int productId = 0,
            int pageIndex = 0, int pageSize = int.MaxValue,
            bool onlyConfirmed = false, bool? isActive = null);

        /// <summary>
        /// Inserts an affiliate
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        void InsertAffiliate(Affiliate affiliate);

        /// <summary>
        /// Updates the affiliate
        /// </summary>
        /// <param name="affiliate">Affiliate</param>
        void UpdateAffiliate(Affiliate affiliate);

        /// <summary>
        /// Get affiliate by identifiers
        /// </summary>
        /// <param name="affiliateIds">Affiliate identifiers</param>
        /// <returns>Order</returns>
        IList<Affiliate> GetAffiliatesByIds(int[] affiliateIds);
    }
}