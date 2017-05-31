using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using System;
using System.Collections.Generic;

namespace Nop.Core.Domain.Affiliates
{
    /// <summary>
    /// Represents an affiliate
    /// </summary>
    public partial class Affiliate : BaseEntity
    {
        private ICollection<Product> _affiliateProducts;

        public Guid Guid { get; set; }
        /// <summary>
        /// Gets or sets the address identifier
        /// </summary>
        public int AddressId { get; set; }

        /// <summary>
        /// Gets or sets the admin comment
        /// </summary>
        public string AdminComment { get; set; }

        /// <summary>
        /// Gets or sets the friendly name for generated affiliate URL (by default affiliate ID is used)
        /// </summary>
        public string FriendlyUrlName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity has been deleted
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the address
        /// </summary>
        public virtual Address Address { get; set; }

        /// <summary>
        /// Gets or sets the affiliate products
        /// </summary>
        public virtual ICollection<Product> AffiliateProducts
        {
            get { return _affiliateProducts ?? (_affiliateProducts = new List<Product>()); }
            set { _affiliateProducts = value; }
        }
    }
}
