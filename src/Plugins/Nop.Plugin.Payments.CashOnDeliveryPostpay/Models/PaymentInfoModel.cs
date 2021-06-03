using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.CashOnDeliveryPostpay.Models
{
    public record PaymentInfoModel : BaseNopModel
    {
        public string DescriptionText { get; set; }
    }
}