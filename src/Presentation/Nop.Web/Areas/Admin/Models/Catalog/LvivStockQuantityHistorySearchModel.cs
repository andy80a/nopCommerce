using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Catalog
{
    public record LvivStockQuantityHistorySearchModel : BaseSearchModel
    {
        [NopResourceDisplayName("Admin.Catalog.Products.Fields.Sku")]
        public string ArticleNumber { get; set; }

        [NopResourceDisplayName("Admin.Orders.List.GoDirectlyToNumber")]
        public string GoDirectlyToCustomOrderNumber { get; set; }
    }
}