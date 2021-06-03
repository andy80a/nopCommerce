using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.CashOnDeliveryPostpay.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.CashOnDeliveryPostpay.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class PaymentCashOnDeliveryPostpayController : BasePaymentController
    {
        #region Fields

        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor

        public PaymentCashOnDeliveryPostpayController(ILanguageService languageService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IStoreContext storeContext)
        {
            _languageService = languageService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _storeContext = storeContext;
        }

        #endregion

        #region Methods

        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var CashOnDeliveryPostpayPaymentSettings = await _settingService.LoadSettingAsync<CashOnDeliveryPostpayPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                DescriptionText = CashOnDeliveryPostpayPaymentSettings.DescriptionText
            };

            //locales
            await AddLocalesAsync(_languageService, model.Locales, async (locale, languageId) =>
            {
                locale.DescriptionText = await _localizationService.GetLocalizedSettingAsync(CashOnDeliveryPostpayPaymentSettings, x => x.DescriptionText, languageId, 0, false, false);
            });

            model.AdditionalFee = CashOnDeliveryPostpayPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = CashOnDeliveryPostpayPaymentSettings.AdditionalFeePercentage;
            model.ShippableProductRequired = CashOnDeliveryPostpayPaymentSettings.ShippableProductRequired;
            model.SkipPaymentInfo = CashOnDeliveryPostpayPaymentSettings.SkipPaymentInfo;
            model.ActiveStoreScopeConfiguration = storeScope;

            if (storeScope > 0)
            {
                model.DescriptionText_OverrideForStore = await _settingService.SettingExistsAsync(CashOnDeliveryPostpayPaymentSettings, x => x.DescriptionText, storeScope);
                model.AdditionalFee_OverrideForStore = await _settingService.SettingExistsAsync(CashOnDeliveryPostpayPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = await _settingService.SettingExistsAsync(CashOnDeliveryPostpayPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.ShippableProductRequired_OverrideForStore = await _settingService.SettingExistsAsync(CashOnDeliveryPostpayPaymentSettings, x => x.ShippableProductRequired, storeScope);
                model.SkipPaymentInfo_OverrideForStore = await _settingService.SettingExistsAsync(CashOnDeliveryPostpayPaymentSettings, x => x.SkipPaymentInfo, storeScope);
            }

            return View("~/Plugins/Payments.CashOnDeliveryPostpay/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var CashOnDeliveryPostpayPaymentSettings = await _settingService.LoadSettingAsync<CashOnDeliveryPostpayPaymentSettings>(storeScope);

            //save settings
            CashOnDeliveryPostpayPaymentSettings.DescriptionText = model.DescriptionText;
            CashOnDeliveryPostpayPaymentSettings.AdditionalFee = model.AdditionalFee;
            CashOnDeliveryPostpayPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            CashOnDeliveryPostpayPaymentSettings.ShippableProductRequired = model.ShippableProductRequired;
            CashOnDeliveryPostpayPaymentSettings.SkipPaymentInfo = model.SkipPaymentInfo;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            await _settingService.SaveSettingOverridablePerStoreAsync(CashOnDeliveryPostpayPaymentSettings, x => x.DescriptionText, model.DescriptionText_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(CashOnDeliveryPostpayPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(CashOnDeliveryPostpayPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(CashOnDeliveryPostpayPaymentSettings, x => x.ShippableProductRequired, model.ShippableProductRequired_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(CashOnDeliveryPostpayPaymentSettings, x => x.SkipPaymentInfo, model.SkipPaymentInfo_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            //localization. no multi-store support for localization yet.
            foreach (var localized in model.Locales)
            {
                await _localizationService.SaveLocalizedSettingAsync(CashOnDeliveryPostpayPaymentSettings, x => x.DescriptionText,
                    localized.LanguageId,
                    localized.DescriptionText);
            }

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        #endregion
    }
}