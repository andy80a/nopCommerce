using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.CashOnDeliveryPostpay.Models
{
    public record ConfigurationLocalizedModel : ILocalizedLocaleModel
    {
        [NopResourceDisplayName("Plugins.Payment.CashOnDeliveryPostpay.DescriptionText")]
        public string DescriptionText { get; set; }

        public int LanguageId { get; set; }
    }
}
