using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Infrastructure;

namespace Nop.Services.Catalog
{
    public static class ProductExtensions
    {
        public static async Task<int> GetLvivStockQuantityAsync(this Product product)
        {
            IProductService _productService = EngineContext.Current.Resolve<IProductService>();
            return await _productService.GetLvivStockQuantityAsync(product);
        }


        public static async Task<string> GetLvivStockQuantityForDisplayAsync(this Product product, IWorkContext workContext)
        {
            var realQ = await product.GetLvivStockQuantityAsync();

            if (product.Price < 200)
            {
                if (realQ < 10)
                {
                    return realQ + " шт.";
                }
            }

            if (product.Price < 1000)
            {
                if (realQ < 5)
                {
                    return realQ + " шт.";
                }
            }
            if (realQ < 3)
            {
                return realQ + " шт.";
            }
            if ( (await workContext.GetWorkingLanguageAsync()).Id == 4)
            {
                return "В наявності";
            }
            return "В наличии";
        }

        /// <summary>
        /// Sorts the elements of a sequence in order according to a product sorting rule
        /// </summary>
        /// <param name="productsQuery">A sequence of products to order</param>
        /// <param name="orderBy">Product sorting rule</param>
        /// <returns>An System.Linq.IOrderedQueryable`1 whose elements are sorted according to a rule.</returns>
        public static IOrderedQueryable<Product> OrderBy(this IQueryable<Product> productsQuery, ProductSortingEnum orderBy) 
        {
            return orderBy switch
            {
                ProductSortingEnum.NameAsc => productsQuery.OrderBy(p => p.Name),
                ProductSortingEnum.NameDesc => productsQuery.OrderByDescending(p => p.Name),
                ProductSortingEnum.PriceAsc => productsQuery.OrderBy(p => p.Price),
                ProductSortingEnum.PriceDesc => productsQuery.OrderByDescending(p => p.Price),
                ProductSortingEnum.CreatedOn => productsQuery.OrderByDescending(p => p.CreatedOnUtc),
                _ => productsQuery.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
            };
        }
    }
}