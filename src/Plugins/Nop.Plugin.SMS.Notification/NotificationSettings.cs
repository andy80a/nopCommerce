
using Nop.Core.Configuration;

namespace Nop.Plugin.SMS.Notification
{
    public class NotificationSettings : ISettings
    {
        /// <summary>
        /// Gets or sets the value indicting whether this SMS provider is enabled
        /// </summary>
        public bool Enabled { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string Sender { get; set; }

        public string MsgNewOrder { get; set; }

        public string MsgNewUser { get; set; }

        public string MsgShipment { get; set; }

        public string MsgCancelledOrder { get; set; }

    }
}