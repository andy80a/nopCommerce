using System;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Orders
{
    public record EkvModel : BaseNopEntityModel
    {
        public override int Id { get; set; }

        public decimal OrderTotal { get; set; }

        public decimal ExchangeRate { get; set; }

        public decimal Total { get; set; }

        public DateTime DateCreated { get; set; }
    }
    public record EkvAggreratorModel : BaseNopModel
    {
        //aggregator properties
        public string aggregatorCount { get; set; }
        public string aggregatorTotalPln { get; set; }
        public string aggregatorTotalUah { get; set; }
    }
}