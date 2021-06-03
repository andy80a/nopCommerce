using System;
using System.Linq;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Core.Plugins;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using TestSendSms;

namespace Nop.Plugin.SMS.Notification
{
    /// <summary>
    /// Represents the Notification SMS provider
    /// </summary>
    public class NotificationSmsProvider : BasePlugin, IMiscPlugin
    {
        private readonly NotificationSettings _NotificationSettings;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly ILogger _logger;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly EmailAccountSettings _emailAccountSettings;

        public NotificationSmsProvider(NotificationSettings NotificationSettings,
            IQueuedEmailService queuedEmailService, 
            IEmailAccountService emailAccountService,
            ILogger logger,
            ISettingService settingService,
            IStoreContext storeContext,
            EmailAccountSettings emailAccountSettings)
        {
            this._NotificationSettings = NotificationSettings;
            this._queuedEmailService = queuedEmailService;
            this._emailAccountService = emailAccountService;
            this._logger = logger;
            this._settingService = settingService;
            this._storeContext = storeContext;
            this._emailAccountSettings = emailAccountSettings;
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "SmsNotification";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.SMS.Notification.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new NotificationSettings();
            //{
            //    Email = "yournumber@vtext.com",
            //};
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Notification.TestFailed", "Test message sending failed");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Notification.TestSuccess", "Test message was sent (queued)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Notification.Fields.Enabled", "Enabled");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Notification.Fields.Enabled.Hint", "Check to enable SMS provider");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Notification.Fields.Email", "Email");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Notification.Fields.Email.Hint", "Notification email address(e.g. your_phone_number@vtext.com)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Notification.Fields.TestMessage", "Message text");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Notification.Fields.TestMessage.Hint", "Text of the test message");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Notification.SendTest", "Send");
            this.AddOrUpdatePluginLocaleResource("Plugins.Sms.Notification.SendTest.Hint", "Send test message");

            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<NotificationSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Sms.Notification.TestFailed");
            this.DeletePluginLocaleResource("Plugins.Sms.Notification.TestSuccess");
            this.DeletePluginLocaleResource("Plugins.Sms.Notification.Fields.Enabled");
            this.DeletePluginLocaleResource("Plugins.Sms.Notification.Fields.Enabled.Hint");
            this.DeletePluginLocaleResource("Plugins.Sms.Notification.Fields.Email");
            this.DeletePluginLocaleResource("Plugins.Sms.Notification.Fields.Email.Hint");
            this.DeletePluginLocaleResource("Plugins.Sms.Notification.Fields.TestMessage");
            this.DeletePluginLocaleResource("Plugins.Sms.Notification.Fields.TestMessage.Hint");
            this.DeletePluginLocaleResource("Plugins.Sms.Notification.SendTest");
            this.DeletePluginLocaleResource("Plugins.Sms.Notification.SendTest.Hint");

            base.Uninstall();
        }
    }
}
