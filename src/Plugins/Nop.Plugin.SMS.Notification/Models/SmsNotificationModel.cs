using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework;

namespace Nop.Plugin.SMS.Notification.Models
{
    public class SmsNotificationModel
    {
        [NopResourceDisplayName("Plugins.Sms.Verizon.Fields.Enabled")]
        public bool Enabled { get; set; }

        [Required]
        [DisplayName("Логін")]
        public string UserName { get; set; }

        [Required]
        [DisplayName("Пароль")]
        public string Password { get; set; }

        [DisplayName("Відправник")]
        [StringLength(11, ErrorMessage = "Не довше 11 символів")]
        public string Sender { get; set; }

        [DataType(DataType.MultilineText)]
        [DisplayName("Повідомлення (нове замовлення)")]
        public string MsgNewOrder { get; set; }

        [DataType(DataType.MultilineText)]
        [DisplayName("Повідомлення (новий користувач)")]
        public string MsgNewUser { get; set; }

        [DataType(DataType.MultilineText)]
        [DisplayName("Повідомлення (замовлення відправлено)")]
        public string MsgShipment { get; set; }

        [DisplayName("Стан рахунку")]
        public string Balance { get; set; }

        [DisplayName("Отримувач")]
        public string TestReceiver { get; set; }

        [DisplayName("Повідомоення")]
        [DataType(DataType.MultilineText)]
        public string Message { get; set; }

        public string TestSmsResult { get; set; }
        [DataType(DataType.MultilineText)]
        [DisplayName("Повідомлення (відмінене замовлення)")]
        public string MsgCancelledOrder { get; set; }
    }
}