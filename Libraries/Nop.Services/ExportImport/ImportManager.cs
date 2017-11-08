using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.WebPages;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.ExportImport.Help;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using OfficeOpenXml;

namespace Nop.Services.ExportImport
{
    /// <summary>
    /// Import manager
    /// </summary>
    public partial class ImportManager : IImportManager
    {
        #region Fields

        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IPictureService _pictureService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IStoreContext _storeContext;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IEncryptionService _encryptionService;
        private readonly IDataProvider _dataProvider;
        private readonly MediaSettings _mediaSettings;
        private readonly IVendorService _vendorService;
        private readonly IProductTemplateService _productTemplateService;
        private readonly IShippingService _shippingService;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly IMeasureService _measureService;
        private readonly CatalogSettings _catalogSettings;

        #endregion

        #region Ctor

        public ImportManager(IProductService productService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            IPictureService pictureService,
            IUrlRecordService urlRecordService,
            IStoreContext storeContext,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            ICountryService countryService,
            IStateProvinceService stateProvinceService,
            IEncryptionService encryptionService,
            IDataProvider dataProvider,
            MediaSettings mediaSettings,
            IVendorService vendorService,
            IProductTemplateService productTemplateService,
            IShippingService shippingService,
            ITaxCategoryService taxCategoryService,
            IMeasureService measureService,
            IProductAttributeService productAttributeService,
            CatalogSettings catalogSettings)
        {
            this._productService = productService;
            this._categoryService = categoryService;
            this._manufacturerService = manufacturerService;
            this._pictureService = pictureService;
            this._urlRecordService = urlRecordService;
            this._storeContext = storeContext;
            this._newsLetterSubscriptionService = newsLetterSubscriptionService;
            this._countryService = countryService;
            this._stateProvinceService = stateProvinceService;
            this._encryptionService = encryptionService;
            this._dataProvider = dataProvider;
            this._mediaSettings = mediaSettings;
            this._vendorService = vendorService;
            this._productTemplateService = productTemplateService;
            this._shippingService = shippingService;
            this._taxCategoryService = taxCategoryService;
            this._measureService = measureService;
            this._productAttributeService = productAttributeService;
            this._catalogSettings = catalogSettings;
        }

        #endregion

        #region Utilities

        protected virtual int GetColumnIndex(string[] properties, string columnName)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

            if (columnName == null)
                throw new ArgumentNullException("columnName");

            for (int i = 0; i < properties.Length; i++)
                if (properties[i].Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                    return i + 1; //excel indexes start from 1
            return 0;
        }

        protected virtual string ConvertColumnToString(object columnValue)
        {
            if (columnValue == null)
                return null;

            return Convert.ToString(columnValue);
        }

        protected virtual string GetMimeTypeFromFilePath(string filePath)
        {
            var mimeType = MimeMapping.GetMimeMapping(filePath);

            //little hack here because MimeMapping does not contain all mappings (e.g. PNG)
            if (mimeType == MimeTypes.ApplicationOctetStream)
                mimeType = MimeTypes.ImageJpeg;

            return mimeType;
        }

        /// <summary>
        /// Creates or loads the image
        /// </summary>
        /// <param name="picturePath">The path to the image file</param>
        /// <param name="name">The name of the object</param>
        /// <param name="picId">Image identifier, may be null</param>
        /// <returns>The image or null if the image has not changed</returns>
        protected virtual Picture LoadPicture(string picturePath, string name, int? picId = null)
        {
            if (String.IsNullOrEmpty(picturePath) || !File.Exists(picturePath))
                return null;

            var mimeType = GetMimeTypeFromFilePath(picturePath);
            var newPictureBinary = File.ReadAllBytes(picturePath);
            var pictureAlreadyExists = false;
            if (picId != null)
            {
                //compare with existing product pictures
                var existingPicture = _pictureService.GetPictureById(picId.Value);

                var existingBinary = _pictureService.LoadPictureBinary(existingPicture);
                //picture binary after validation (like in database)
                var validatedPictureBinary = _pictureService.ValidatePicture(newPictureBinary, mimeType);
                if (existingBinary.SequenceEqual(validatedPictureBinary) ||
                    existingBinary.SequenceEqual(newPictureBinary))
                {
                    pictureAlreadyExists = true;
                }
            }

            if (pictureAlreadyExists) return null;

            var newPicture = _pictureService.InsertPicture(newPictureBinary, mimeType,
                _pictureService.GetPictureSeName(name));
            return newPicture;
        }

        protected virtual void ImportProductImagesUsingServices(IList<ProductPictureMetadata> productPictureMetadata)
        {
            foreach (var product in productPictureMetadata)
            {
                foreach (var picturePath in new[] { product.Picture1Path, product.Picture2Path, product.Picture3Path })
                {
                    if (String.IsNullOrEmpty(picturePath))
                        continue;

                    var mimeType = GetMimeTypeFromFilePath(picturePath);
                    var newPictureBinary = File.ReadAllBytes(picturePath);
                    var pictureAlreadyExists = false;
                    if (!product.IsNew)
                    {
                        //compare with existing product pictures
                        var existingPictures = _pictureService.GetPicturesByProductId(product.ProductItem.Id);
                        foreach (var existingPicture in existingPictures)
                        {
                            var existingBinary = _pictureService.LoadPictureBinary(existingPicture);
                            //picture binary after validation (like in database)
                            var validatedPictureBinary = _pictureService.ValidatePicture(newPictureBinary, mimeType);
                            if (!existingBinary.SequenceEqual(validatedPictureBinary) &&
                                !existingBinary.SequenceEqual(newPictureBinary))
                                continue;
                            //the same picture content
                            pictureAlreadyExists = true;
                            break;
                        }
                    }

                    if (pictureAlreadyExists)
                        continue;
                    var newPicture = _pictureService.InsertPicture(newPictureBinary, mimeType, _pictureService.GetPictureSeName(product.ProductItem.Name));
                    product.ProductItem.ProductPictures.Add(new ProductPicture
                    {
                        //EF has some weird issue if we set "Picture = newPicture" instead of "PictureId = newPicture.Id"
                        //pictures are duplicated
                        //maybe because entity size is too large
                        PictureId = newPicture.Id,
                        DisplayOrder = 1,
                    });
                    _productService.UpdateProduct(product.ProductItem);
                }
            }
        }

        protected virtual void ImportProductImagesUsingHash(IList<ProductPictureMetadata> productPictureMetadata, IList<Product> allProductsBySku)
        {
            //performance optimization, load all pictures hashes
            //it will only be used if the images are stored in the SQL Server database (not compact)
            var takeCount = _dataProvider.SupportedLengthOfBinaryHash() - 1;
            var productsImagesIds = _productService.GetProductsImagesIds(allProductsBySku.Select(p => p.Id).ToArray());
            var allPicturesHashes = _pictureService.GetPicturesHash(productsImagesIds.SelectMany(p => p.Value).ToArray());

            foreach (var product in productPictureMetadata)
            {
                foreach (var picturePath in new[] { product.Picture1Path, product.Picture2Path, product.Picture3Path })
                {
                    if (String.IsNullOrEmpty(picturePath))
                        continue;

                    var mimeType = GetMimeTypeFromFilePath(picturePath);
                    var newPictureBinary = File.ReadAllBytes(picturePath);
                    var pictureAlreadyExists = false;
                    if (!product.IsNew)
                    {
                        var newImageHash = _encryptionService.CreateHash(newPictureBinary.Take(takeCount).ToArray());
                        var newValidatedImageHash = _encryptionService.CreateHash(_pictureService.ValidatePicture(newPictureBinary, mimeType).Take(takeCount).ToArray());

                        var imagesIds = productsImagesIds.ContainsKey(product.ProductItem.Id)
                            ? productsImagesIds[product.ProductItem.Id]
                            : new int[0];

                        pictureAlreadyExists = allPicturesHashes.Where(p => imagesIds.Contains(p.Key)).Select(p => p.Value).Any(p => p == newImageHash || p == newValidatedImageHash);
                    }

                    if (pictureAlreadyExists)
                        continue;
                    var newPicture = _pictureService.InsertPicture(newPictureBinary, mimeType, _pictureService.GetPictureSeName(product.ProductItem.Name));
                    product.ProductItem.ProductPictures.Add(new ProductPicture
                    {
                        //EF has some weird issue if we set "Picture = newPicture" instead of "PictureId = newPicture.Id"
                        //pictures are duplicated
                        //maybe because entity size is too large
                        PictureId = newPicture.Id,
                        DisplayOrder = 1,
                    });
                    _productService.UpdateProduct(product.ProductItem);
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Import products from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        public virtual void ImportProductsFromXlsx(Stream stream)
        {
            //var start = DateTime.Now;
            using (var xlPackage = new ExcelPackage(stream))
            {
                // get the first worksheet in the workbook
                var worksheet = xlPackage.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    throw new NopException("No worksheet found");

                //the columns
                var properties = new List<PropertyByName<Product>>();
                var poz = 1;
                while (true)
                {
                    try
                    {
                        var cell = worksheet.Cells[1, poz];

                        if (cell == null || cell.Value == null || String.IsNullOrEmpty(cell.Value.ToString()))
                            break;

                        poz += 1;
                        properties.Add(new PropertyByName<Product>(cell.Value.ToString()));
                    }
                    catch
                    {
                       break;
                    }
                }

                var manager = new PropertyManager<Product>(properties.ToArray());
                
                var endRow = 2;
                var allCategoriesIds = new List<int>();
                var allSku = new List<string>();

                var tempProperty = manager.GetProperty("CategoryIds");
                var categoryCellNum = tempProperty.Return(p => p.PropertyOrderPosition, -1);

                //tempProperty = manager.GetProperty("SKU");
                //var skuCellNum = tempProperty.Return(p => p.PropertyOrderPosition, -1);

                //var allManufacturersNames = new List<string>();
                //tempProperty = manager.GetProperty("Manufacturers");
                //var manufacturerCellNum = tempProperty.Return(p => p.PropertyOrderPosition, -1);

                //manager.SetSelectList("ProductType", ProductType.SimpleProduct.ToSelectList(useLocalization: false));
                //manager.SetSelectList("GiftCardType", GiftCardType.Virtual.ToSelectList(useLocalization: false));
                //manager.SetSelectList("DownloadActivationType", DownloadActivationType.Manually.ToSelectList(useLocalization: false));
                //manager.SetSelectList("ManageInventoryMethod", ManageInventoryMethod.DontManageStock.ToSelectList(useLocalization: false));
                //manager.SetSelectList("LowStockActivity", LowStockActivity.Nothing.ToSelectList(useLocalization: false));
                //manager.SetSelectList("BackorderMode", BackorderMode.NoBackorders.ToSelectList(useLocalization: false));
                //manager.SetSelectList("RecurringCyclePeriod", RecurringProductCyclePeriod.Days.ToSelectList(useLocalization: false));
                //manager.SetSelectList("RentalPricePeriod", RentalPricePeriod.Days.ToSelectList(useLocalization: false));

                //manager.SetSelectList("Vendor", _vendorService.GetAllVendors(showHidden: true).Select(v => v as BaseEntity).ToSelectList(p => (p as Vendor).Return(v => v.Name, String.Empty)));
                manager.SetSelectList("PageTemplate", _productTemplateService.GetAllProductTemplates().Select(pt => pt as BaseEntity).ToSelectList(p => (p as ProductTemplate).Return(pt => pt.Name, String.Empty)));
                //manager.SetSelectList("DeliveryDate", _shippingService.GetAllDeliveryDates().Select(dd => dd as BaseEntity).ToSelectList(p => (p as DeliveryDate).Return(dd => dd.Name, String.Empty)));
                //manager.SetSelectList("TaxCategory", _taxCategoryService.GetAllTaxCategories().Select(tc => tc as BaseEntity).ToSelectList(p => (p as TaxCategory).Return(tc => tc.Name, String.Empty)));
                //manager.SetSelectList("BasepriceUnit", _measureService.GetAllMeasureWeights().Select(mw => mw as BaseEntity).ToSelectList(p =>(p as MeasureWeight).Return(mw => mw.Name, String.Empty)));
                //manager.SetSelectList("BasepriceBaseUnit", _measureService.GetAllMeasureWeights().Select(mw => mw as BaseEntity).ToSelectList(p => (p as MeasureWeight).Return(mw => mw.Name, String.Empty)));

                //var allAttributeIds = new List<int>();
                //var attributeIdCellNum = managerProductAttribute.GetProperty("AttributeId").PropertyOrderPosition + ExportProductAttribute.ProducAttributeCellOffset;
                
                //find end of data
                while (true)
                {
                    var allColumnsAreEmpty = manager.GetProperties
                        .Select(property => worksheet.Cells[endRow, property.PropertyOrderPosition])
                        .All(cell => cell == null || cell.Value == null || String.IsNullOrEmpty(cell.Value.ToString()));

                    if (allColumnsAreEmpty)
                        break;

                    if (categoryCellNum > 0)
                    { 
                        var categoryIds = worksheet.Cells[endRow, categoryCellNum].Value.Return(p => p.ToString(), string.Empty);

                        if (!categoryIds.IsEmpty())
                            try
                            {
                                var idArr=categoryIds.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
                                foreach (var idstr in idArr)
                                {
                                    int categoryId = Convert.ToInt32(idstr);
                                    allCategoriesIds.Add(categoryId);
                                }
                                
                            }
                            catch { }
                    }

                    
                    endRow++;
                }

                //performance optimization, the check for the existence of the categories in one SQL request
                var notExistingSeNames = _categoryService.GetNotExistingCategories(allCategoriesIds.ToArray());
                if (notExistingSeNames.Any())
                {
                    throw new ArgumentException(string.Format("The following category Id(s) don't exist - {0}", string.Join(", ", notExistingSeNames)));
                }

                ////performance optimization, the check for the existence of the manufacturers in one SQL request
                //var notExistingManufacturers = _manufacturerService.GetNotExistingManufacturers(allManufacturersNames.ToArray());
                //if (notExistingManufacturers.Any())
                //{
                //    throw new ArgumentException(string.Format("The following manufacturer name(s) don't exist - {0}", string.Join(", ", notExistingManufacturers)));
                //}

                ////performance optimization, the check for the existence of the product attributes in one SQL request
                //var notExistingProductAttributes = _productAttributeService.GetNotExistingAttributes(allAttributeIds.ToArray());
                //if (notExistingProductAttributes.Any())
                //{
                //    throw new ArgumentException(string.Format("The following product attribute ID(s) don't exist - {0}", string.Join(", ", notExistingProductAttributes)));
                //}

                ////performance optimization, load all products by SKU in one SQL request
                //var allProductsBySku = _productService.GetProductsBySku(allSku.ToArray());

                ////performance optimization, load all categories IDs for products in one SQL request
                //var allProductsCategoryIds = _categoryService.GetProductCategoryIds(allProductsBySku.Select(p => p.Id).ToArray());

                //performance optimization, load all categories in one SQL request
//                var allCategories = _categoryService.GetAllCategories(showHidden: true);
                

                ////performance optimization, load all manufacturers IDs for products in one SQL request
                //var allProductsManufacturerIds = _manufacturerService.GetProductManufacturerIds(allProductsBySku.Select(p => p.Id).ToArray());

                ////performance optimization, load all manufacturers in one SQL request
                //var allManufacturers = _manufacturerService.GetAllManufacturers(showHidden: true);

                //product to import images
                var productPictureMetadata = new List<ProductPictureMetadata>();

                //Product lastLoadedProduct = null;

                for (var iRow = 2; iRow < endRow; iRow++)
                {
                    
                    manager.ReadFromXlsx(worksheet, iRow);

                    //var product = skuCellNum > 0 ? allProductsBySku.FirstOrDefault(p => p.Sku == manager.GetProperty("SKU").StringValue) : null;

                    //var isNew = product == null;                    
                    //product = product ?? new Product();
                    var pid=manager.GetProperty("Id").IntValue;
                    var isNew = pid > 0 ? false : true;                                    
                    var product= new Product();
                    if (isNew)
                        product.CreatedOnUtc = DateTime.UtcNow;
                    else
                    {
                        product = _productService.GetProductById(pid);
                        if (product == null)
                        {
                            product = new Product();
                            isNew = false;
                        }
                    }

                    foreach (var property in manager.GetProperties)
                    {
                        switch (property.PropertyName)
                        {
                            //case "ProductType":
                            //    product.ProductTypeId = property.IntValue;
                            //    break;
                            //case "ParentGroupedProductId":
                            //    product.ParentGroupedProductId = property.IntValue;
                            //    break;
                            //case "VisibleIndividually":
                            //    product.VisibleIndividually = property.BooleanValue;
                            //    break;                            
                            case "Name":
                                product.Name = property.StringValue;
                                break;
                            case "GroupName":
                                product.ShortDescription = property.StringValue;
                                break;
                            case "FullDescription":
                                product.FullDescription = property.StringValue;
                                break;
                            
                            case "PageTemplate":
                                product.ProductTemplateId = property.IntValue;
                                break;
                            
                            case "Published":
                                product.Published = property.BooleanValue;
                                break;
                            
                            case "HasPdfDownload":
                                product.IsDownload = property.BooleanValue;
                                break;
                            case "PdfDownloadId":
                                product.DownloadId = property.IntValue;
                                break;                            
                            case "HasVedioDownload":
                                product.HasSampleDownload = property.BooleanValue;
                                break;
                            case "VedioDownloadId":
                                product.SampleDownloadId = property.IntValue;
                                break;
                            case "DisplayOrder":
                                product.DisplayOrder = property.IntValue;
                                break;
                                
                        }
                    }

                    //set default product type id
                    if (isNew)
                        product.ProductType = ProductType.SimpleProduct;

                    product.UpdatedOnUtc = DateTime.UtcNow;

                    if (isNew)
                    {
                        _productService.InsertProduct(product);
                        
                        tempProperty = manager.GetProperty("CategoryIds");
                        if (tempProperty != null)
                        {
                            var categoryIdsStr = tempProperty.StringValue;

                            //新的根據分類SeName添加文章所屬分類記錄的邏輯
                            var categoryids = categoryIdsStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var idstr in categoryids)
                            {
                                int id = Convert.ToInt32(idstr);
                                var category = _categoryService.GetCategoryById(id);
                                if (category != null)
                                {
                                    var productCategory = new ProductCategory
                                    {
                                        ProductId = product.Id,
                                        CategoryId = category.Id,
                                        IsFeaturedProduct = false,
                                        DisplayOrder = product.DisplayOrder
                                    };
                                    _categoryService.InsertProductCategory(productCategory);
                                }
                            }                            
                        }
                    }
                    else
                    {
                        tempProperty = manager.GetProperty("CategoryIds");
                        if (tempProperty != null)
                        {
                            var categoryIdsStr = tempProperty.StringValue;
                            List<ProductCategory> removeProductCategoryList = new List<ProductCategory>();
                            //刪除現有的文章所有的分類
                            foreach (var item in product.ProductCategories)
                            {
                                removeProductCategoryList.Add(item);
                            }
                            foreach (var item in removeProductCategoryList)
                            {
                                _categoryService.DeleteProductCategory(item);
                            }

                            //新的根據分類SeName添加文章所屬分類記錄的邏輯
                            var categoryids = categoryIdsStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var idstr in categoryids)
                            {
                                int id = Convert.ToInt32(idstr);
                                var category = _categoryService.GetCategoryById(id);
                                if (category != null)
                                {
                                    var productCategory = new ProductCategory
                                    {
                                        ProductId = product.Id,
                                        CategoryId = category.Id,
                                        IsFeaturedProduct = false,
                                        DisplayOrder = product.DisplayOrder
                                    };
                                    _categoryService.InsertProductCategory(productCategory);
                                }
                            }

                        }
                                                                                               
                        _productService.UpdateProduct(product);
                    }

                    
                }                               
            }
            
        }
        
        /// <summary>
        /// Import newsletter subscribers from TXT file
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>Number of imported subscribers</returns>
        public virtual int ImportNewsletterSubscribersFromTxt(Stream stream)
        {
            int count = 0;
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    string[] tmp = line.Split(',');

                    string email;
                    bool isActive = true;
                    int storeId = _storeContext.CurrentStore.Id;
                    //parse
                    if (tmp.Length == 1)
                    {
                        //"email" only
                        email = tmp[0].Trim();
                    }
                    else if (tmp.Length == 2)
                    {
                        //"email" and "active" fields specified
                        email = tmp[0].Trim();
                        isActive = Boolean.Parse(tmp[1].Trim());
                    }
                    else if (tmp.Length == 3)
                    {
                        //"email" and "active" and "storeId" fields specified
                        email = tmp[0].Trim();
                        isActive = Boolean.Parse(tmp[1].Trim());
                        storeId = Int32.Parse(tmp[2].Trim());
                    }
                    else
                        throw new NopException("Wrong file format");

                    //import
                    var subscription = _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(email, storeId);
                    if (subscription != null)
                    {
                        subscription.Email = email;
                        subscription.Active = isActive;
                        _newsLetterSubscriptionService.UpdateNewsLetterSubscription(subscription);
                    }
                    else
                    {
                        subscription = new NewsLetterSubscription
                        {
                            Active = isActive,
                            CreatedOnUtc = DateTime.UtcNow,
                            Email = email,
                            StoreId = storeId,
                            NewsLetterSubscriptionGuid = Guid.NewGuid()
                        };
                        _newsLetterSubscriptionService.InsertNewsLetterSubscription(subscription);
                    }
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Import states from TXT file
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>Number of imported states</returns>
        public virtual int ImportStatesFromTxt(Stream stream)
        {
            int count = 0;
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    string[] tmp = line.Split(',');

                    if (tmp.Length != 5)
                        throw new NopException("Wrong file format");

                    //parse
                    var countryTwoLetterIsoCode = tmp[0].Trim();
                    var name = tmp[1].Trim();
                    var abbreviation = tmp[2].Trim();
                    bool published = Boolean.Parse(tmp[3].Trim());
                    int displayOrder = Int32.Parse(tmp[4].Trim());

                    var country = _countryService.GetCountryByTwoLetterIsoCode(countryTwoLetterIsoCode);
                    if (country == null)
                    {
                        //country cannot be loaded. skip
                        continue;
                    }

                    //import
                    var states = _stateProvinceService.GetStateProvincesByCountryId(country.Id, showHidden: true);
                    var state = states.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

                    if (state != null)
                    {
                        state.Abbreviation = abbreviation;
                        state.Published = published;
                        state.DisplayOrder = displayOrder;
                        _stateProvinceService.UpdateStateProvince(state);
                    }
                    else
                    {
                        state = new StateProvince
                        {
                            CountryId = country.Id,
                            Name = name,
                            Abbreviation = abbreviation,
                            Published = published,
                            DisplayOrder = displayOrder,
                        };
                        _stateProvinceService.InsertStateProvince(state);
                    }
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Import manufacturers from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        public virtual void ImportManufacturersFromXlsx(Stream stream)
        {
            //property array
            var properties = new[]
            {
                new PropertyByName<Manufacturer>("Id"),
                new PropertyByName<Manufacturer>("Name"),
                new PropertyByName<Manufacturer>("Description"),
                new PropertyByName<Manufacturer>("ManufacturerTemplateId"),
                new PropertyByName<Manufacturer>("MetaKeywords"),
                new PropertyByName<Manufacturer>("MetaDescription"),
                new PropertyByName<Manufacturer>("MetaTitle"),
                new PropertyByName<Manufacturer>("SeName"),
                new PropertyByName<Manufacturer>("Picture"),
                new PropertyByName<Manufacturer>("PageSize"),
                new PropertyByName<Manufacturer>("AllowCustomersToSelectPageSize"),
                new PropertyByName<Manufacturer>("PageSizeOptions"),
                new PropertyByName<Manufacturer>("PriceRanges"),
                new PropertyByName<Manufacturer>("Published"),
                new PropertyByName<Manufacturer>("DisplayOrder")
            };

            var manager = new PropertyManager<Manufacturer>(properties);

            using (var xlPackage = new ExcelPackage(stream))
            {
                // get the first worksheet in the workbook
                var worksheet = xlPackage.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    throw new NopException("No worksheet found");

                var iRow = 2;

                while (true)
                {
                    var allColumnsAreEmpty = manager.GetProperties
                        .Select(property => worksheet.Cells[iRow, property.PropertyOrderPosition])
                        .All(cell => cell == null || cell.Value == null || String.IsNullOrEmpty(cell.Value.ToString()));

                    if (allColumnsAreEmpty)
                        break;

                    manager.ReadFromXlsx(worksheet, iRow);

                    var manufacturer = _manufacturerService.GetManufacturerById(manager.GetProperty("Id").IntValue);

                    var isNew = manufacturer == null;

                    manufacturer = manufacturer ?? new Manufacturer();

                    if (isNew)
                        manufacturer.CreatedOnUtc = DateTime.UtcNow;

                    manufacturer.Name = manager.GetProperty("Name").StringValue;
                    manufacturer.Description = manager.GetProperty("Description").StringValue;
                    manufacturer.ManufacturerTemplateId = manager.GetProperty("ManufacturerTemplateId").IntValue;
                    manufacturer.MetaKeywords = manager.GetProperty("MetaKeywords").StringValue;
                    manufacturer.MetaDescription = manager.GetProperty("MetaDescription").StringValue;
                    manufacturer.MetaTitle = manager.GetProperty("MetaTitle").StringValue;
                    var picture = LoadPicture(manager.GetProperty("Picture").StringValue, manufacturer.Name,
                        isNew ? null : (int?) manufacturer.PictureId);
                    manufacturer.PageSize = manager.GetProperty("PageSize").IntValue;
                    manufacturer.AllowCustomersToSelectPageSize = manager.GetProperty("AllowCustomersToSelectPageSize").BooleanValue;
                    manufacturer.PageSizeOptions = manager.GetProperty("PageSizeOptions").StringValue;
                    manufacturer.PriceRanges = manager.GetProperty("PriceRanges").StringValue;
                    manufacturer.Published = manager.GetProperty("Published").BooleanValue;
                    manufacturer.DisplayOrder = manager.GetProperty("DisplayOrder").IntValue;

                    if (picture != null)
                        manufacturer.PictureId = picture.Id;

                    manufacturer.UpdatedOnUtc = DateTime.UtcNow;

                    if (isNew)
                        _manufacturerService.InsertManufacturer(manufacturer);
                    else
                        _manufacturerService.UpdateManufacturer(manufacturer);

                    //search engine name
                    var seName = manager.GetProperty("SeName").StringValue;
                    _urlRecordService.SaveSlug(manufacturer, manufacturer.ValidateSeName(seName, manufacturer.Name, true), 0);

                    iRow++;
                }
            }
        }

        /// <summary>
        /// Import categories from XLSX file
        /// </summary>
        /// <param name="stream">Stream</param>
        public virtual void ImportCategoriesFromXlsx(Stream stream)
        {
            var properties = new[]
            {
                new PropertyByName<Category>("Id"),
                new PropertyByName<Category>("Name"),
                new PropertyByName<Category>("Description"),
                new PropertyByName<Category>("CategoryTemplateId"),
                new PropertyByName<Category>("MetaKeywords"),
                new PropertyByName<Category>("MetaDescription"),
                new PropertyByName<Category>("MetaTitle"),
                new PropertyByName<Category>("SeName"),
                new PropertyByName<Category>("ParentCategoryId"),
                new PropertyByName<Category>("Picture"),
                new PropertyByName<Category>("PageSize"),
                new PropertyByName<Category>("AllowCustomersToSelectPageSize"),
                new PropertyByName<Category>("PageSizeOptions"),
                new PropertyByName<Category>("PriceRanges"),
                new PropertyByName<Category>("ShowOnHomePage"),
                new PropertyByName<Category>("IncludeInTopMenu"),
                new PropertyByName<Category>("Published"),
                new PropertyByName<Category>("DisplayOrder")
            };

            var manager = new PropertyManager<Category>(properties);

            using (var xlPackage = new ExcelPackage(stream))
            {
                // get the first worksheet in the workbook
                var worksheet = xlPackage.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    throw new NopException("No worksheet found");

                var iRow = 2;

                while (true)
                {
                    var allColumnsAreEmpty = manager.GetProperties
                        .Select(property => worksheet.Cells[iRow, property.PropertyOrderPosition])
                        .All(cell => cell == null || cell.Value == null || String.IsNullOrEmpty(cell.Value.ToString()));

                    if (allColumnsAreEmpty)
                        break;

                    manager.ReadFromXlsx(worksheet, iRow);

                    var category = _categoryService.GetCategoryById(manager.GetProperty("Id").IntValue);

                    var isNew = category == null;

                    category = category ?? new Category();

                    if (isNew)
                        category.CreatedOnUtc = DateTime.UtcNow;

                    category.Name = manager.GetProperty("Name").StringValue;
                    category.Description = manager.GetProperty("Description").StringValue;

                    category.CategoryTemplateId = manager.GetProperty("CategoryTemplateId").IntValue;
                    category.MetaKeywords = manager.GetProperty("MetaKeywords").StringValue;
                    category.MetaDescription = manager.GetProperty("MetaDescription").StringValue;
                    category.MetaTitle = manager.GetProperty("MetaTitle").StringValue;
                    category.ParentCategoryId = manager.GetProperty("ParentCategoryId").IntValue;
                    var picture = LoadPicture(manager.GetProperty("Picture").StringValue, category.Name, isNew ? null : (int?) category.PictureId);
                    category.PageSize = manager.GetProperty("PageSize").IntValue;
                    category.AllowCustomersToSelectPageSize = manager.GetProperty("AllowCustomersToSelectPageSize").BooleanValue;
                    category.PageSizeOptions = manager.GetProperty("PageSizeOptions").StringValue;
                    category.PriceRanges = manager.GetProperty("PriceRanges").StringValue;
                    category.ShowOnHomePage = manager.GetProperty("ShowOnHomePage").BooleanValue;
                    category.IncludeInTopMenu = manager.GetProperty("IncludeInTopMenu").BooleanValue;
                    category.Published = manager.GetProperty("Published").BooleanValue;
                    category.DisplayOrder = manager.GetProperty("DisplayOrder").IntValue;

                    if (picture != null)
                        category.PictureId = picture.Id;

                    category.UpdatedOnUtc = DateTime.UtcNow;

                    if (isNew)
                        _categoryService.InsertCategory(category);
                    else
                        _categoryService.UpdateCategory(category);

                    //search engine name
                    var seName = manager.GetProperty("SeName").StringValue;
                    _urlRecordService.SaveSlug(category, category.ValidateSeName(seName, category.Name, true), 0);

                    iRow++;
                }
            }
        }

        #endregion

        #region Nested classes

        protected class ProductPictureMetadata
        {
            public Product ProductItem { get; set; }
            public string Picture1Path { get; set; }
            public string Picture2Path { get; set; }
            public string Picture3Path { get; set; }
            public bool IsNew { get; set; }
        }

        #endregion
    }
}
