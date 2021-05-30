using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Web.Models.Catalog
{
    public partial record DeliveryPriceModel : BaseNopModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductShortDescription { get; set; }
        public double ProductPrice { get; set; }
        public string ProductSeName { get; set; }
        public string DeliveryType { get; set; }

        [NopResourceDisplayName("Address.Fields.City")]
        public int CityId { get; set; }
        public IList<SelectListItem> Cities { get; set; }
        public double Weight { get; set; }
        public double VolumeWeight { get; set; }
        public double DeliveryPrice { get; set; }
        public double DeliveryPriceHome { get; set; }
        public int IsObr { get; set; }
    }
}
