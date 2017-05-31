using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Nop.Web.Models.Media;

namespace Nop.Web.Models.Catalog
{
    public partial class ProductDetailsModel : BaseNopEntityModel
    {
        public ProductDetailsModel()
        {
            DefaultPictureModel = new PictureModel();
            PictureModels = new List<PictureModel>();
            ProductSpecifications = new List<ProductSpecificationModel>();            
        }
        //picture(s)
        public bool DefaultPictureZoomEnabled { get; set; }
        public PictureModel DefaultPictureModel { get; set; }
        public IList<PictureModel> PictureModels { get; set; }

        public string Name { get; set; }
        public string ShortDescription { get; set; }
        public string FullDescription { get; set; }
        public string ProductTemplateViewPath { get; set; }        
        public string SeName { get; set; }
        public bool IsDownload { get; set; }
        public int DownloadId { get; set; }        
        public bool HasSampleDownload { get; set; }        
        public int SampleDownloadId { get; set; }
        public string SampleDownloadType { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime UpdatedOnUtc { get; set; }

        public ProductType ProductType { get; set; }

        
        public IList<ProductSpecificationModel> ProductSpecifications { get; set; }

        
    }
}