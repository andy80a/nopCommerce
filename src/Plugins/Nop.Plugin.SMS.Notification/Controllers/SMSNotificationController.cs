using System;
using System.Web.Mvc;
using Nop.Core.Plugins;
using Nop.Plugin.SMS.Notification.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;
using TestSendSms;

namespace Nop.Plugin.SMS.Notification.Controllers
{
    [AdminAuthorize]
    public class SMSNotificationController : BasePluginController
    {
        private readonly NotificationSettings _pluginSettings;
        private readonly ISettingService _settingService;
        private readonly IPluginFinder _pluginFinder;
        private readonly ILocalizationService _localizationService;

        public SMSNotificationController(NotificationSettings pluginSettings,
            ISettingService settingService, IPluginFinder pluginFinder,
            ILocalizationService localizationService)
        {
            this._pluginSettings = pluginSettings;
            this._settingService = settingService;
            this._pluginFinder = pluginFinder;
            this._localizationService = localizationService;
        }

        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new SmsNotificationModel
            {
                Enabled = _pluginSettings.Enabled,
                UserName = _pluginSettings.UserName,
                Password = _pluginSettings.Password,
                Sender = _pluginSettings.Sender,
                MsgNewOrder = _pluginSettings.MsgNewOrder,
                MsgCancelledOrder = _pluginSettings.MsgCancelledOrder,
                MsgNewUser = _pluginSettings.MsgNewUser,
                MsgShipment = _pluginSettings.MsgShipment
            };
            var balance = new XmlSmsProvider(model.UserName, model.Password).Balance();
            if (!balance.HasError())
            {
                model.Balance = string.Format("{0} кредитів", balance.credits);
            }
            else
            {
                model.Balance = balance.GetError();
            }
            return View("~/Plugins/SMS.Notification/Views/SmsNotification/Configure.cshtml", model);
        }

        [ChildActionOnly]
        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        public ActionResult ConfigurePOST(SmsNotificationModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            //save settings
            _pluginSettings.Enabled = model.Enabled;
            _pluginSettings.UserName = model.UserName;
            _pluginSettings.Password = model.Password;
            _pluginSettings.Sender = model.Sender;
            _pluginSettings.MsgNewOrder = model.MsgNewOrder;
            _pluginSettings.MsgCancelledOrder = model.MsgCancelledOrder;
            _pluginSettings.MsgNewUser = model.MsgNewUser;
            _pluginSettings.MsgShipment = model.MsgShipment;
            _settingService.SaveSetting(_pluginSettings);

            return View("~/Plugins/SMS.Notification/Views/SmsNotification/Configure.cshtml", model);

        }

        [ChildActionOnly]
        [HttpPost, ActionName("Configure")]
        [FormValueRequired("test-sms")]
        public ActionResult TestSms(SmsNotificationModel model)
        {
            if (String.IsNullOrEmpty(model.Message) || String.IsNullOrEmpty(model.TestReceiver))
            {
                model.TestSmsResult = "Введіть отримувача та повідомлення";
            }
            else
            {
                new XmlSmsProvider(_pluginSettings.UserName, _pluginSettings.Password)
                    .Send(_pluginSettings.Sender, model.Message, model.TestReceiver);
            }
            return View("~/Plugins/SMS.Notification/Views/SmsNotification/Configure.cshtml", model);
            
        }
    }
}