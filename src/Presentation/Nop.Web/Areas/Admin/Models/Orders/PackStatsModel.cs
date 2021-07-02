using System.ComponentModel;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Orders
{
    public record PackStatsModel : BaseNopEntityModel
    {
        [DisplayName("Пакувальник")]
        public string Name { get; set; }

        [DisplayName("Вага")]
        public decimal Weight { get; set; }

        [DisplayName("Об'ємна Вага")]
        public decimal VolumeWeight { get; set; }

        [DisplayName("Посилки")]
        public decimal PackageCount { get; set; }

        [DisplayName("Артикули")]
        public decimal ItemsCount { get; set; }
    }
}