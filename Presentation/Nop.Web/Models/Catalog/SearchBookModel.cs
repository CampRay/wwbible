using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Catalog
{
    public partial class SearchBookModel : BaseNopModel
    {
        public SearchBookModel()
        {                     
            this.Verses = new List<VerseModel>();
        }

        public string Warning { get; set; }

        public bool NoResults { get; set; }

        /// <summary>
        /// Query string
        /// </summary>
        [NopResourceDisplayName("Search.SearchTerm")]
        [AllowHtml]
        public string q { get; set; }
        /// <summary>
        /// Category ID
        /// </summary>
        [NopResourceDisplayName("Search.Category")]
        public int cid { get; set; }
        [NopResourceDisplayName("Search.IncludeSubCategories")]
        public bool isc { get; set; }
        
        public string BibleName { get; set; }

        public IList<VerseModel> Verses { get; set; }

        #region Nested classes

        public class VerseModel : BaseNopEntityModel
        {
            public string BibleName { get; set; }
            public int CategoryId { get; set; }
            public string BookName { get; set; }
            public string Chapter { get; set; }
            public string VerseNo { get; set; }
            public string VerseText { get; set; }            
            public string BookUrl { get; set; }            
        }

        #endregion
    }
}