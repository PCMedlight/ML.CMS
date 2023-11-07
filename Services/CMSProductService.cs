using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Configuration;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.Catalog;

namespace ML.CMS.Services
{
    public class CMSProductService
    {
        private readonly SmartDbContext _context;
        private readonly IAclService _aclService;
        private readonly ICategoryService _categoryService;
        private readonly CatalogHelper _productCatalogHelper;

        public CMSProductService(SmartDbContext context, IAclService aclService, ICategoryService categoryService, CatalogHelper productCatalogHelper)
        {
            _context = context;
            _aclService = aclService;
            _categoryService = categoryService;
            _productCatalogHelper = productCatalogHelper;
        }

        public async Task<List<Dictionary<string, object>>> GetProductsAsync(int ThumbnailSize)
        {
            List<Product> products = _context.Products
                .Where(product => product.Visibility != ProductVisibility.Hidden && product.Published == true)
                .ToList();

            List<Product> authorizedProducts = new List<Product>();

            foreach (var product in products)
            {
                if (await _aclService.AuthorizeAsync(product))
                {
                    authorizedProducts.Add(product);
                }
            }
            ProductSummaryModel productCatalog = await getProductsFromCatalog(ThumbnailSize, authorizedProducts);

            List<Dictionary<string, object>> productList = new List<Dictionary<string, object>>();
            foreach (var product in productCatalog.Items)
            {
                Dictionary<string, object> productData = new Dictionary<string, object>();
                productData["Name"] = product.Name;
                productData["Image"] = product.Image.Url;
                productData["Price"] = product.Price;
                productData["Url"] = product.DetailUrl;
                productData["DisableBuyButton"] = product.Price.DisableBuyButton;
                productData["ShortDescription"] = product.ShortDescription;
                var productCategories = await _categoryService.GetProductCategoriesByProductIdsAsync(new[] { product.Id });
                var categoryNames = productCategories.Select(pc => pc.Category.Name).ToList();

                productData["MainCategory"] = categoryNames.FirstOrDefault();
                productData["AllCategories"] = string.Join(",", categoryNames);
                productList.Add(productData);
            }

            return productList;
        }

        private async Task<ProductSummaryModel> getProductsFromCatalog(int ThumbnailSize, List<Product> authorizedProducts)
        {
            ProductSummaryMappingSettings settings = new ProductSummaryMappingSettings
            {
                MapPrices = true,
                MapPictures = true,
                MapAttributes = true,
                MapShortDescription = true,
                MapReviews = true,
                ThumbnailSize = ThumbnailSize,
                PrefetchTranslations = true,
                PrefetchUrlSlugs = true
            };

            ProductSummaryModel productCatalog = await _productCatalogHelper.MapProductSummaryModelAsync(authorizedProducts, settings);
            return productCatalog;
        }

        public record CategoryInfo(string Name, int DisplayOrder);

        public List<CategoryInfo> GetCategoriesFromProducts(List<Dictionary<string, object>> productList)
        {
            List<CategoryInfo> allCategories = new List<CategoryInfo>();

            foreach (var product in productList)
            {
                var categoryNames = (string)product["AllCategories"];
                var categories = categoryNames.Split(',').Select(name => new CategoryInfo(name, 0)); // Assuming DisplayOrder is not important here
                allCategories = allCategories.Union(categories).ToList();
            }

            return allCategories.OrderBy(category => category.DisplayOrder).ToList();
        }
    }

}
