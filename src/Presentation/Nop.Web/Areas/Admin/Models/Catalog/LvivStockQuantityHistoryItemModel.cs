using System;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Catalog
{
    public record LvivStockQuantityHistoryItemModel : BaseNopEntityModel
    {
        public string ArticleNumber { get; set; }
        public int? OrderId { get; set; }

        /// <summary>
        /// Gets or sets the stock quantity adjustment
        /// </summary>
        public int QuantityAdjustment { get; set; }

        /// <summary>
        /// Gets or sets current stock quantity
        /// </summary>
        public int QuantityReservedSum { get; set; }

        public int QuantityAdjustmentSum { get; set; }

        /// <summary>
        /// Gets or sets current stock quantity
        /// </summary>
        public int QuantityReserved { get; set; }

        /// <summary>
        /// Gets or sets the message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the date and time of instance creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
    }
}