using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Pickup;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Web.Models.Checkout;
using Nop.Web.Models.Common;
using static Nop.Web.Models.Checkout.CheckoutBillingAddressModel;

namespace Nop.Web.Factories
{
    public partial class CheckoutModelFactory : ICheckoutModelFactory
    {
        #region Fields

        public static int UkrainianLanguageId = 4;

        private readonly INopDataProvider _dataProvider;
        private readonly AddressSettings _addressSettings;
        private readonly CommonSettings _commonSettings;
        private readonly IAddressModelFactory _addressModelFactory;
        private readonly IAddressService _addressService;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IPaymentService _paymentService;
        private readonly IPickupPluginManager _pickupPluginManager;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IRewardPointService _rewardPointService;
        private readonly IShippingPluginManager _shippingPluginManager;
        private readonly IShippingService _shippingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;
        private readonly OrderSettings _orderSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly ShippingSettings _shippingSettings;

        #endregion

        #region Ctor

        public CheckoutModelFactory(
            INopDataProvider dataProvider,
            AddressSettings addressSettings,
            CommonSettings commonSettings,
            IAddressModelFactory addressModelFactory,
            IAddressService addressService,
            ICountryService countryService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IOrderProcessingService orderProcessingService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IPaymentPluginManager paymentPluginManager,
            IPaymentService paymentService,
            IPickupPluginManager pickupPluginManager,
            IPriceFormatter priceFormatter,
            IRewardPointService rewardPointService,
            IShippingPluginManager shippingPluginManager,
            IShippingService shippingService,
            IShoppingCartService shoppingCartService,
            IStateProvinceService stateProvinceService,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            ITaxService taxService,
            IWorkContext workContext,
            OrderSettings orderSettings,
            PaymentSettings paymentSettings,
            RewardPointsSettings rewardPointsSettings,
            ShippingSettings shippingSettings)
        {
            _dataProvider = dataProvider;
            _addressSettings = addressSettings;
            _commonSettings = commonSettings;
            _addressModelFactory = addressModelFactory;
            _addressService = addressService;
            _countryService = countryService;
            _currencyService = currencyService;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _orderProcessingService = orderProcessingService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _paymentPluginManager = paymentPluginManager;
            _paymentService = paymentService;
            _pickupPluginManager = pickupPluginManager;
            _priceFormatter = priceFormatter;
            _rewardPointService = rewardPointService;
            _shippingPluginManager = shippingPluginManager;
            _shippingService = shippingService;
            _shoppingCartService = shoppingCartService;
            _stateProvinceService = stateProvinceService;
            _storeContext = storeContext;
            _storeMappingService = storeMappingService;
            _taxService = taxService;
            _workContext = workContext;
            _orderSettings = orderSettings;
            _paymentSettings = paymentSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _shippingSettings = shippingSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Prepares the checkout pickup points model
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the checkout pickup points model
        /// </returns>
        protected virtual async Task<CheckoutPickupPointsModel> PrepareCheckoutPickupPointsModelAsync(IList<ShoppingCartItem> cart)
        {
            var model = new CheckoutPickupPointsModel
            {
                AllowPickupInStore = _shippingSettings.AllowPickupInStore
            };

            if (!model.AllowPickupInStore)
                return model;

            model.DisplayPickupPointsOnMap = _shippingSettings.DisplayPickupPointsOnMap;
            model.GoogleMapsApiKey = _shippingSettings.GoogleMapsApiKey;
            var pickupPointProviders = await _pickupPluginManager.LoadActivePluginsAsync(await _workContext.GetCurrentCustomerAsync(), (await _storeContext.GetCurrentStoreAsync()).Id);
            if (pickupPointProviders.Any())
            {
                var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;
                var pickupPointsResponse = await _shippingService.GetPickupPointsAsync((await _workContext.GetCurrentCustomerAsync()).BillingAddressId ?? 0,
                    await _workContext.GetCurrentCustomerAsync(), storeId: (await _storeContext.GetCurrentStoreAsync()).Id);
                if (pickupPointsResponse.Success)
                    model.PickupPoints = await pickupPointsResponse.PickupPoints.SelectAwait(async point =>
                    {
                        var country = await _countryService.GetCountryByTwoLetterIsoCodeAsync(point.CountryCode);
                        var state = await _stateProvinceService.GetStateProvinceByAbbreviationAsync(point.StateAbbreviation, country?.Id);

                        var pickupPointModel = new CheckoutPickupPointModel
                        {
                            Id = point.Id,
                            Name = point.Name,
                            Description = point.Description,
                            ProviderSystemName = point.ProviderSystemName,
                            Address = point.Address,
                            City = point.City,
                            County = point.County,
                            StateName = state != null ? await _localizationService.GetLocalizedAsync(state, x => x.Name, languageId) : string.Empty,
                            CountryName = country != null ? await _localizationService.GetLocalizedAsync(country, x => x.Name, languageId) : string.Empty,
                            ZipPostalCode = point.ZipPostalCode,
                            Latitude = point.Latitude,
                            Longitude = point.Longitude,
                            OpeningHours = point.OpeningHours
                        };

                        var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, (await _storeContext.GetCurrentStoreAsync()).Id);
                        var amount = await _orderTotalCalculationService.IsFreeShippingAsync(cart) ? 0 : point.PickupFee;

                        if (amount > 0)
                        {
                            (amount, _) = await _taxService.GetShippingPriceAsync(amount, await _workContext.GetCurrentCustomerAsync());
                            amount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(amount, await _workContext.GetWorkingCurrencyAsync());
                            pickupPointModel.PickupFee = await _priceFormatter.FormatShippingPriceAsync(amount, true);
                        }

                        //adjust rate
                        var (shippingTotal, _) = await _orderTotalCalculationService.AdjustShippingRateAsync(point.PickupFee, cart, true);
                        var (rateBase, _) = await _taxService.GetShippingPriceAsync(shippingTotal, await _workContext.GetCurrentCustomerAsync());
                        var rate = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(rateBase, await _workContext.GetWorkingCurrencyAsync());
                        pickupPointModel.PickupFee = await _priceFormatter.FormatShippingPriceAsync(rate, true);

                        return pickupPointModel;
                    }).ToListAsync();
                else
                    foreach (var error in pickupPointsResponse.Errors)
                        model.Warnings.Add(error);
            }

            //only available pickup points
            var shippingProviders = await _shippingPluginManager.LoadActivePluginsAsync(await _workContext.GetCurrentCustomerAsync(), (await _storeContext.GetCurrentStoreAsync()).Id);
            if (!shippingProviders.Any())
            {
                if (!pickupPointProviders.Any())
                {
                    model.Warnings.Add(await _localizationService.GetResourceAsync("Checkout.ShippingIsNotAllowed"));
                    model.Warnings.Add(await _localizationService.GetResourceAsync("Checkout.PickupPoints.NotAvailable"));
                }
                model.PickupInStoreOnly = true;
                model.PickupInStore = true;
                return model;
            }


            return model;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare billing address model
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="selectedCountryId">Selected country identifier</param>
        /// <param name="prePopulateNewAddressWithCustomerFields">Pre populate new address with customer fields</param>
        /// <param name="overrideAttributesXml">Override attributes xml</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the billing address model
        /// </returns>
        public virtual async Task<CheckoutBillingAddressModel> PrepareBillingAddressModelAsync(IList<ShoppingCartItem> cart,
            int? selectedCountryId = null,
            bool prePopulateNewAddressWithCustomerFields = false,
            string overrideAttributesXml = "")
        {
            var model = new CheckoutBillingAddressModel
            {
                ShipToSameAddressAllowed = _shippingSettings.ShipToSameAddress && await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart),
                //allow customers to enter (choose) a shipping address if "Disable Billing address step" setting is enabled
                ShipToSameAddress = !_orderSettings.DisableBillingAddressCheckoutStep
            };

            //existing addresses
            var addresses = await (await _customerService.GetAddressesByCustomerIdAsync((await _workContext.GetCurrentCustomerAsync()).Id))
                .WhereAwait(async a => !a.CountryId.HasValue || await _countryService.GetCountryByAddressAsync(a) is Country country &&
                    (//published
                    country.Published &&
                    //allow billing
                    country.AllowsBilling &&
                    //enabled for the current store
                    await _storeMappingService.AuthorizeAsync(country)))
                .ToListAsync();
            foreach (var address in addresses)
            {
                var addressModel = new AddressModel();
                await _addressModelFactory.PrepareAddressModelAsync(addressModel,
                    address: address,
                    excludeProperties: false,
                    addressSettings: _addressSettings);

                if (await _addressService.IsAddressValidAsync(address))
                {
                    model.ExistingAddresses.Add(addressModel);
                }
                else
                {
                    model.InvalidExistingAddresses.Add(addressModel);
                }
            }

            //new address
            model.BillingNewAddress.CountryId = selectedCountryId;
            await _addressModelFactory.PrepareAddressModelAsync(model.BillingNewAddress,
                address: null,
                excludeProperties: false,
                addressSettings: _addressSettings,
                loadCountries: async () => await _countryService.GetAllCountriesForBillingAsync((await _workContext.GetWorkingLanguageAsync()).Id),
                prePopulateWithCustomerFields: prePopulateNewAddressWithCustomerFields,
                customer: await _workContext.GetCurrentCustomerAsync(),
                overrideAttributesXml: overrideAttributesXml);
            return model;
        }

        /// <summary>
        /// Prepare shipping address model
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="selectedCountryId">Selected country identifier</param>
        /// <param name="prePopulateNewAddressWithCustomerFields">Pre populate new address with customer fields</param>
        /// <param name="overrideAttributesXml">Override attributes xml</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping address model
        /// </returns>
        public virtual async Task<CheckoutShippingAddressModel> PrepareShippingAddressModelAsync(
            AddressType type, int weight, int novaPoshtaRegionId, int novaPoshtaCityId,
            IList<ShoppingCartItem> cart,
            int? selectedCountryId = null, bool prePopulateNewAddressWithCustomerFields = false, string overrideAttributesXml = "")
        {
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();
            var workingLanguage = await _workContext.GetWorkingLanguageAsync();

            var model = new CheckoutShippingAddressModel
            {
                DisplayPickupInStore = !_orderSettings.DisplayPickupInStoreOnShippingMethodPage,
                Type = type
            };

            if (!_orderSettings.DisplayPickupInStoreOnShippingMethodPage)
                model.PickupPointsModel = await PrepareCheckoutPickupPointsModelAsync(cart);

            //existing addresses
            var addresses = await _customerService.GetAddressesByCustomerIdAsync(currentCustomer.Id);
            //.WhereAwait(async a => !a.CountryId.HasValue || await _countryService.GetCountryByAddressAsync(a) is Country country &&
            //    (//published
            //    country.Published &&
            //    //allow shipping
            //    country.AllowsShipping &&
            //    //enabled for the current store
            //    await _storeMappingService.AuthorizeAsync(country)))
            //.ToListAsync();
            foreach (var address in addresses)
            {
                var addressModel = new AddressModel();
                await _addressModelFactory.PrepareAddressModelAsync(addressModel,
                    address: address,
                    excludeProperties: false,
                    addressSettings: _addressSettings,
                    type: type);

                if (await _addressService.IsAddressValidAsync(address))
                {
                    model.ExistingAddresses.Add(addressModel);
                }
                else
                {
                    model.InvalidExistingAddresses.Add(addressModel);
                }
            }

            //new address
            model.ShippingNewAddress.CountryId = selectedCountryId;
            await _addressModelFactory.PrepareAddressModelAsync(model.ShippingNewAddress,
                address: null,
                excludeProperties: false,
                addressSettings: _addressSettings,
                loadCountries: async () => await _countryService.GetAllCountriesForShippingAsync((await _workContext.GetWorkingLanguageAsync()).Id),
                prePopulateWithCustomerFields: prePopulateNewAddressWithCustomerFields,
                customer: currentCustomer,
                overrideAttributesXml: overrideAttributesXml,
                type: type);






            if (type == AddressType.NovaPoshtaWarehouse)
            {
                model.ExistingAddresses.Clear();
                model.ShippingNewAddress.NovaPoshtaCity = new List<SelectListItem>();
                model.ShippingNewAddress.NovaPoshtaWarehouse = new List<SelectListItem>();

                var selectedShippingOption = await _genericAttributeService.GetAttributeAsync<ShippingOption>(currentCustomer, NopCustomerDefaults.SelectedShippingOptionAttribute, (await _storeContext.GetCurrentStoreAsync()).Id);

                if (selectedShippingOption.Name.ToLower().Contains("обр"))
                {
                    weight = Math.Max(31, weight);
                }
                model.ShippingNewAddress.Weight = weight;
                model.ShippingNewAddress.NovaPoshtaCityId = novaPoshtaCityId;//київ
                if (novaPoshtaCityId == 0)
                {
                    if (currentCustomer != null)
                    {
                        var cityId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.NovaPoshtaCityId);

                        if (cityId > 0)
                        {
                            model.ShippingNewAddress.NovaPoshtaCityId = cityId;
                        }
                    }
                }

                if (workingLanguage.Id == UkrainianLanguageId)
                {
                    PrepareNovaPoshtaWarehouseInfoUa(weight, model);
                }
                else
                {
                    PrepareNovaPoshtaWarehouseInfoRu(weight, model);
                }
            }
            else if (type == AddressType.NovaPoshtaAddress)
            {
                model.ExistingAddresses.Clear();
                model.ShippingNewAddress.NovaPoshtaCity = new List<SelectListItem>();
                model.ShippingNewAddress.NovaPoshtaStreet = new List<SelectListItem>();
                model.ShippingNewAddress.Weight = weight;

                model.ShippingNewAddress.NovaPoshtaCityId = novaPoshtaCityId;//київ
                if (novaPoshtaCityId == 0)
                {
                    if (currentCustomer != null)
                    {
                        var cityId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.NovaPoshtaCityId);

                        if (cityId > 0)
                        {
                            model.ShippingNewAddress.NovaPoshtaCityId = cityId;
                        }

                        var streetId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.NovaPoshtaStreetId);
                        if (streetId > 0)
                        {
                            model.ShippingNewAddress.NovaPoshtaStreetId = streetId;
                        }
                    }
                }

                if (workingLanguage.Id == UkrainianLanguageId)
                {
                    PrepareNovaPoshtaStreetInfoUa(model);
                }
                else
                {
                    PrepareNovaPoshtaStreetInfoRu(model);
                }
            }
            else///////////////////////////////
               if (type == AddressType.SATWarehouse)
            {
                model.ExistingAddresses.Clear();
                model.ShippingNewAddress.NovaPoshtaCity = new List<SelectListItem>();
                model.ShippingNewAddress.NovaPoshtaWarehouse = new List<SelectListItem>();

                model.ShippingNewAddress.Weight = weight;
                model.ShippingNewAddress.NovaPoshtaCityId = novaPoshtaCityId;//київ
                if (novaPoshtaCityId == 0)
                {
                    if (currentCustomer != null)
                    {
                        var cityId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.SATCityId);

                        if (cityId != 0)
                        {
                            model.ShippingNewAddress.NovaPoshtaCityId = cityId;
                        }
                    }
                }

                PrepareSATWarehouseInfo(model);

            }
            else if (type == AddressType.SATAddress)
            {
                model.ExistingAddresses.Clear();
                model.ShippingNewAddress.NovaPoshtaCity = new List<SelectListItem>();
                model.ShippingNewAddress.NovaPoshtaStreet = new List<SelectListItem>();
                model.ShippingNewAddress.Weight = weight;
                model.ShippingNewAddress.NovaPoshtaCityId = novaPoshtaCityId;
                if (novaPoshtaRegionId == 0)
                {
                    if (currentCustomer != null)
                    {
                        var cityId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.SATCityId);
                        if (cityId != 0)
                        {
                            model.ShippingNewAddress.NovaPoshtaCityId = cityId;
                        }

                        var streetId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.SATStreetId);
                        if (streetId != 0)
                        {
                            model.ShippingNewAddress.NovaPoshtaStreetId = streetId;
                        }
                    }
                }

                PrepareSATInfo(model);
            }
            else///////////////////////////////
               if (type == AddressType.MeestWarehouse)
            {
                model.ExistingAddresses.Clear();
                model.ShippingNewAddress.NovaPoshtaCity = new List<SelectListItem>();
                model.ShippingNewAddress.NovaPoshtaWarehouse = new List<SelectListItem>();

                model.ShippingNewAddress.Weight = weight;
                model.ShippingNewAddress.NovaPoshtaCityId = novaPoshtaCityId;//київ
                if (novaPoshtaCityId == 0)
                {
                    if (currentCustomer != null)
                    {
                        var cityId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.MeestCityId);

                        if (cityId > 0)
                        {
                            model.ShippingNewAddress.NovaPoshtaCityId = cityId;
                        }
                    }
                }

                PrepareMeestWarehouseInfo(weight, model);
            }
            else if (type == AddressType.MeestAddress)
            {
                model.ExistingAddresses.Clear();
                model.ShippingNewAddress.NovaPoshtaRegion = new List<SelectListItem>();
                model.ShippingNewAddress.NovaPoshtaCity = new List<SelectListItem>();
                model.ShippingNewAddress.NovaPoshtaStreet = new List<SelectListItem>();
                model.ShippingNewAddress.Weight = weight;

                model.ShippingNewAddress.NovaPoshtaRegionId = novaPoshtaRegionId;//київ
                model.ShippingNewAddress.NovaPoshtaCityId = novaPoshtaCityId;
                if (novaPoshtaRegionId == 0)
                {
                    if (currentCustomer != null)
                    {
                        var regionId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.MeestRegionId);

                        if (regionId > 0)
                        {
                            model.ShippingNewAddress.NovaPoshtaRegionId = regionId;
                        }

                        var cityId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.MeestCityId);
                        if (cityId > 0)
                        {
                            model.ShippingNewAddress.NovaPoshtaCityId = cityId;
                        }

                        var streetId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.MeestStreetId);
                        if (streetId > 0)
                        {
                            model.ShippingNewAddress.NovaPoshtaStreetId = streetId;
                        }
                    }
                }

                PrepareMeestRegionInfo(model);
            }


            return model;
        }

        /// <summary>
        /// Prepare shipping method model
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="shippingAddress">Shipping address</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the shipping method model
        /// </returns>
        public virtual async Task<CheckoutShippingMethodModel> PrepareShippingMethodModelAsync(IList<ShoppingCartItem> cart, Address shippingAddress)
        {
            var model = new CheckoutShippingMethodModel
            {
                DisplayPickupInStore = _orderSettings.DisplayPickupInStoreOnShippingMethodPage
            };

            if (_orderSettings.DisplayPickupInStoreOnShippingMethodPage)
                model.PickupPointsModel = await PrepareCheckoutPickupPointsModelAsync(cart);

            var getShippingOptionResponse = await _shippingService.GetShippingOptionsAsync(cart, shippingAddress, await _workContext.GetCurrentCustomerAsync(), storeId: (await _storeContext.GetCurrentStoreAsync()).Id);
            if (getShippingOptionResponse.Success)
            {
                //performance optimization. cache returned shipping options.
                //we'll use them later (after a customer has selected an option).
                await _genericAttributeService.SaveAttributeAsync(await _workContext.GetCurrentCustomerAsync(),
                                                       NopCustomerDefaults.OfferedShippingOptionsAttribute,
                                                       getShippingOptionResponse.ShippingOptions,
                                                       (await _storeContext.GetCurrentStoreAsync()).Id);

                foreach (var shippingOption in getShippingOptionResponse.ShippingOptions)
                {
                    var soModel = new CheckoutShippingMethodModel.ShippingMethodModel
                    {
                        Name = shippingOption.Name,
                        Description = shippingOption.Description,
                        ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName,
                        ShippingOption = shippingOption,
                    };

                    //adjust rate
                    var (shippingTotal, _) = await _orderTotalCalculationService.AdjustShippingRateAsync(shippingOption.Rate, cart, shippingOption.IsPickupInStore);

                    var (rateBase, _) = await _taxService.GetShippingPriceAsync(shippingTotal, await _workContext.GetCurrentCustomerAsync());
                    var rate = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(rateBase, await _workContext.GetWorkingCurrencyAsync());
                    soModel.Fee = await _priceFormatter.FormatShippingPriceAsync(rate, true);

                    model.ShippingMethods.Add(soModel);
                }

                //find a selected (previously) shipping method
                var selectedShippingOption = await _genericAttributeService.GetAttributeAsync<ShippingOption>(await _workContext.GetCurrentCustomerAsync(),
                        NopCustomerDefaults.SelectedShippingOptionAttribute, (await _storeContext.GetCurrentStoreAsync()).Id);
                if (selectedShippingOption != null)
                {
                    var shippingOptionToSelect = model.ShippingMethods.ToList()
                        .Find(so =>
                           !string.IsNullOrEmpty(so.Name) &&
                           so.Name.Equals(selectedShippingOption.Name, StringComparison.InvariantCultureIgnoreCase) &&
                           !string.IsNullOrEmpty(so.ShippingRateComputationMethodSystemName) &&
                           so.ShippingRateComputationMethodSystemName.Equals(selectedShippingOption.ShippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase));
                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }
                //if no option has been selected, let's do it for the first one
                if (model.ShippingMethods.FirstOrDefault(so => so.Selected) == null)
                {
                    var shippingOptionToSelect = model.ShippingMethods.FirstOrDefault();
                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }

                //notify about shipping from multiple locations
                if (_shippingSettings.NotifyCustomerAboutShippingFromMultipleLocations)
                {
                    model.NotifyCustomerAboutShippingFromMultipleLocations = getShippingOptionResponse.ShippingFromMultipleLocations;
                }
            }
            else
            {
                foreach (var error in getShippingOptionResponse.Errors)
                    model.Warnings.Add(error);
            }

            return model;
        }

        /// <summary>
        /// Prepare payment method model
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="filterByCountryId">Filter by country identifier</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the payment method model
        /// </returns>
        public virtual async Task<CheckoutPaymentMethodModel> PreparePaymentMethodModelAsync(IList<ShoppingCartItem> cart, int filterByCountryId)
        {
            var model = new CheckoutPaymentMethodModel();

            //reward points
            if (_rewardPointsSettings.Enabled && !await _shoppingCartService.ShoppingCartIsRecurringAsync(cart))
            {
                var rewardPointsBalance = await _rewardPointService.GetRewardPointsBalanceAsync((await _workContext.GetCurrentCustomerAsync()).Id, (await _storeContext.GetCurrentStoreAsync()).Id);
                rewardPointsBalance = _rewardPointService.GetReducedPointsBalance(rewardPointsBalance);

                var rewardPointsAmountBase = await _orderTotalCalculationService.ConvertRewardPointsToAmountAsync(rewardPointsBalance);
                var rewardPointsAmount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(rewardPointsAmountBase, await _workContext.GetWorkingCurrencyAsync());
                if (rewardPointsAmount > decimal.Zero &&
                    _orderTotalCalculationService.CheckMinimumRewardPointsToUseRequirement(rewardPointsBalance))
                {
                    model.DisplayRewardPoints = true;
                    model.RewardPointsAmount = await _priceFormatter.FormatPriceAsync(rewardPointsAmount, true, false);
                    model.RewardPointsBalance = rewardPointsBalance;

                    //are points enough to pay for entire order? like if this option (to use them) was selected
                    model.RewardPointsEnoughToPayForOrder = !await _orderProcessingService.IsPaymentWorkflowRequiredAsync(cart, true);
                }
            }

            //filter by country
            var paymentMethods = await (await _paymentPluginManager
                .LoadActivePluginsAsyncAsync(await _workContext.GetCurrentCustomerAsync(), (await _storeContext.GetCurrentStoreAsync()).Id, filterByCountryId))
                .Where(pm => pm.PaymentMethodType == PaymentMethodType.Standard || pm.PaymentMethodType == PaymentMethodType.Redirection)
                .WhereAwait(async pm => !await pm.HidePaymentMethodAsync(cart))
                .ToListAsync();
            foreach (var pm in paymentMethods)
            {
                if (await _shoppingCartService.ShoppingCartIsRecurringAsync(cart) && pm.RecurringPaymentType == RecurringPaymentType.NotSupported)
                    continue;

                var pmModel = new CheckoutPaymentMethodModel.PaymentMethodModel
                {
                    Name = await _localizationService.GetLocalizedFriendlyNameAsync(pm, (await _workContext.GetWorkingLanguageAsync()).Id),
                    Description = _paymentSettings.ShowPaymentMethodDescriptions ? await pm.GetPaymentMethodDescriptionAsync() : string.Empty,
                    PaymentMethodSystemName = pm.PluginDescriptor.SystemName,
                    LogoUrl = await _paymentPluginManager.GetPluginLogoUrlAsync(pm)
                };
                //payment method additional fee
                var paymentMethodAdditionalFee = await _paymentService.GetAdditionalHandlingFeeAsync(cart, pm.PluginDescriptor.SystemName);
                var (rateBase, _) = await _taxService.GetPaymentMethodAdditionalFeeAsync(paymentMethodAdditionalFee, await _workContext.GetCurrentCustomerAsync());
                var rate = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(rateBase, await _workContext.GetWorkingCurrencyAsync());
                if (rate > decimal.Zero)
                    pmModel.Fee = await _priceFormatter.FormatPaymentMethodAdditionalFeeAsync(rate, true);

                model.PaymentMethods.Add(pmModel);
            }

            //find a selected (previously) payment method
            var selectedPaymentMethodSystemName = await _genericAttributeService.GetAttributeAsync<string>(await _workContext.GetCurrentCustomerAsync(),
                NopCustomerDefaults.SelectedPaymentMethodAttribute, (await _storeContext.GetCurrentStoreAsync()).Id);
            if (!string.IsNullOrEmpty(selectedPaymentMethodSystemName))
            {
                var paymentMethodToSelect = model.PaymentMethods.ToList()
                    .Find(pm => pm.PaymentMethodSystemName.Equals(selectedPaymentMethodSystemName, StringComparison.InvariantCultureIgnoreCase));
                if (paymentMethodToSelect != null)
                    paymentMethodToSelect.Selected = true;
            }
            //if no option has been selected, let's do it for the first one
            if (model.PaymentMethods.FirstOrDefault(so => so.Selected) == null)
            {
                var paymentMethodToSelect = model.PaymentMethods.FirstOrDefault();
                if (paymentMethodToSelect != null)
                    paymentMethodToSelect.Selected = true;
            }

            return model;
        }

        /// <summary>
        /// Prepare payment info model
        /// </summary>
        /// <param name="paymentMethod">Payment method</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the payment info model
        /// </returns>
        public virtual Task<CheckoutPaymentInfoModel> PreparePaymentInfoModelAsync(IPaymentMethod paymentMethod)
        {
            return Task.FromResult(new CheckoutPaymentInfoModel
            {
                PaymentViewComponentName = paymentMethod.GetPublicViewComponentName(),
                DisplayOrderTotals = _orderSettings.OnePageCheckoutDisplayOrderTotalsOnPaymentInfoTab
            });
        }

        /// <summary>
        /// Prepare confirm order model
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the confirm order model
        /// </returns>
        public virtual async Task<CheckoutConfirmModel> PrepareConfirmOrderModelAsync(IList<ShoppingCartItem> cart)
        {
            var model = new CheckoutConfirmModel
            {
                //terms of service
                TermsOfServiceOnOrderConfirmPage = _orderSettings.TermsOfServiceOnOrderConfirmPage,
                TermsOfServicePopup = _commonSettings.PopupForTermsOfServiceLinks
            };
            //min order amount validation
            var minOrderTotalAmountOk = await _orderProcessingService.ValidateMinOrderTotalAmountAsync(cart);
            if (!minOrderTotalAmountOk)
            {
                var minOrderTotalAmount = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(_orderSettings.MinOrderTotalAmount, await _workContext.GetWorkingCurrencyAsync());
                model.MinOrderTotalWarning = string.Format(await _localizationService.GetResourceAsync("Checkout.MinOrderTotalAmount"), await _priceFormatter.FormatPriceAsync(minOrderTotalAmount, true, false));
            }
            return model;
        }

        /// <summary>
        /// Prepare checkout completed model
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the checkout completed model
        /// </returns>
        public virtual Task<CheckoutCompletedModel> PrepareCheckoutCompletedModelAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var model = new CheckoutCompletedModel
            {
                OrderId = order.Id,
                OnePageCheckoutEnabled = _orderSettings.OnePageCheckoutEnabled,
                CustomOrderNumber = order.CustomOrderNumber
            };

            return Task.FromResult(model);
        }

        /// <summary>
        /// Prepare checkout progress model
        /// </summary>
        /// <param name="step">Step</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the checkout progress model
        /// </returns>
        public virtual Task<CheckoutProgressModel> PrepareCheckoutProgressModelAsync(CheckoutProgressStep step)
        {
            var model = new CheckoutProgressModel { CheckoutProgressStep = step };

            return Task.FromResult(model);
        }

        /// <summary>
        /// Prepare one page checkout model
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the one page checkout model
        /// </returns>
        public virtual async Task<OnePageCheckoutModel> PrepareOnePageCheckoutModelAsync(IList<ShoppingCartItem> cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            var model = new OnePageCheckoutModel
            {
                ShippingRequired = await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart),
                DisableBillingAddressCheckoutStep = _orderSettings.DisableBillingAddressCheckoutStep && (await _customerService.GetAddressesByCustomerIdAsync((await _workContext.GetCurrentCustomerAsync()).Id)).Any(),
                BillingAddress = await PrepareBillingAddressModelAsync(cart, prePopulateNewAddressWithCustomerFields: true)
            };
            return model;
        }

        #endregion
















        private async Task PrepareNovaPoshtaWarehouseInfoUa(int weight, CheckoutShippingAddressModel model)
        {
            var cities = await _dataProvider.QueryAsync<NovaPoshtaCity>(@"SELECT 
      [cityId] as Id, [nameUa] as Name
     FROM NovaPoshta_City ORDER BY [nameUa]");
            model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
            {
                Text = "Місто",
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaCityId == 0
            });
            foreach (var c in cities)
            {
                model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.ShippingNewAddress.NovaPoshtaCityId
                });
            }
            var warehouses = await _dataProvider.QueryAsync<NovaPoshtaWarehouse>(@"SELECT 
      [cityId] as CityId
      ,[wareId] as WareId
      ,[addressUa] as Address
   
     FROM NovaPoshta_Warehouse WHERE cityId = " + model.ShippingNewAddress.NovaPoshtaCityId +
                                                                     " AND max_weight_allowed > " + weight
                                                                       + " ORDER BY Number");

            model.ShippingNewAddress.NovaPoshtaWarehouse.Add(new SelectListItem()
            {
                Text = await _localizationService.GetResourceAsync("Custom.Warehouse"),
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaWarehouseId == 0
            });
            foreach (var c in warehouses)
            {
                model.ShippingNewAddress.NovaPoshtaWarehouse.Add(new SelectListItem()
                {
                    Text = c.Address,
                    Value = c.WareId.ToString(),
                    Selected = c.WareId == model.ShippingNewAddress.NovaPoshtaWarehouseId
                });
            }
        }

        private async Task PrepareSATWarehouseInfo(CheckoutShippingAddressModel model)
        {
            var cities = await _dataProvider.QueryAsync<NovaPoshtaCity>(@"SELECT 
      [cityId] as Id, [name] as Name
     FROM SAT_City 
	 ORDER BY [name]");
            model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
            {
                Text = "Місто",
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaCityId == 0
            });
            foreach (var c in cities)
            {
                model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.ShippingNewAddress.NovaPoshtaCityId
                });
            }
            var warehouses = await _dataProvider.QueryAsync<NovaPoshtaWarehouse>(@"SELECT 
      [cityId] as CityId
      ,[wareId] as WareId
      ,[address] as Address
   
     FROM SAT_Warehouse WHERE cityId = " + model.ShippingNewAddress.NovaPoshtaCityId + " ORDER BY Number");

            model.ShippingNewAddress.NovaPoshtaWarehouse.Add(new SelectListItem()
            {
                Text = await _localizationService.GetResourceAsync("Custom.Warehouse"),
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaWarehouseId == 0
            });
            foreach (var c in warehouses)
            {
                model.ShippingNewAddress.NovaPoshtaWarehouse.Add(new SelectListItem()
                {
                    Text = c.Address,
                    Value = c.WareId.ToString(),
                    Selected = c.WareId == model.ShippingNewAddress.NovaPoshtaWarehouseId
                });
            }
        }

        private async Task PrepareMeestWarehouseInfo(int weight, CheckoutShippingAddressModel model)
        {
            var cities = await _dataProvider.QueryAsync<NovaPoshtaCity>(@"SELECT 
      [cityId] as Id, [nameUa] as Name
     FROM Meest_City 
	 WHERE CityId in (SELECT CityId FROM Meest_Warehouse)
	 ORDER BY [nameUa]");
            model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
            {
                Text = "Місто",
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaCityId == 0
            });
            foreach (var c in cities)
            {
                model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.ShippingNewAddress.NovaPoshtaCityId
                });
            }
            var warehouses = await _dataProvider.QueryAsync<NovaPoshtaWarehouse>(@"SELECT 
      [cityId] as CityId
      ,[wareId] as WareId
      ,[addressUa] as Address
   
     FROM Meest_Warehouse WHERE cityId = " + model.ShippingNewAddress.NovaPoshtaCityId +
                                                                     " AND max_weight_allowed > " + weight
                                                                       + " ORDER BY [addressUa]");

            model.ShippingNewAddress.NovaPoshtaWarehouse.Add(new SelectListItem()
            {
                Text = await _localizationService.GetResourceAsync("Custom.Warehouse"),
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaWarehouseId == 0
            });
            foreach (var c in warehouses)
            {
                model.ShippingNewAddress.NovaPoshtaWarehouse.Add(new SelectListItem()
                {
                    Text = c.Address,
                    Value = c.WareId.ToString(),
                    Selected = c.WareId == model.ShippingNewAddress.NovaPoshtaWarehouseId
                });
            }
        }

        private async Task PrepareNovaPoshtaWarehouseInfoRu(int weight, CheckoutShippingAddressModel model)
        {
            var cities = await _dataProvider.QueryAsync<NovaPoshtaCity>(@"SELECT 
      [cityId] as Id, [nameRu] as Name
     FROM NovaPoshta_City ORDER BY [nameRu]");
            model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
            {
                Text = "Город",
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaCityId == 0
            });
            foreach (var c in cities)
            {
                model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.ShippingNewAddress.NovaPoshtaCityId
                });
            }
            var warehouses = await _dataProvider.QueryAsync<NovaPoshtaWarehouse>(@"SELECT 
      [cityId] as CityId
      ,[wareId] as WareId
      ,[addressRu] as Address
         FROM NovaPoshta_Warehouse WHERE cityId = " + model.ShippingNewAddress.NovaPoshtaCityId +
                                                                     " AND max_weight_allowed > " + weight
                                                                       + " ORDER BY Number");

            model.ShippingNewAddress.NovaPoshtaWarehouse.Add(new SelectListItem()
            {
                Text = await _localizationService.GetResourceAsync("Custom.Warehouse"),
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaWarehouseId == 0
            });
            foreach (var c in warehouses)
            {
                model.ShippingNewAddress.NovaPoshtaWarehouse.Add(new SelectListItem()
                {
                    Text = c.Address,
                    Value = c.WareId.ToString(),
                    Selected = c.WareId == model.ShippingNewAddress.NovaPoshtaWarehouseId
                });
            }
        }

        public class NovaPoshtaCity
        {
            public int Id { get; set; }
            public int RegionId { get; set; }
            public string Name { get; set; }
        }

        public class NovaPoshtaRegion
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }


        public class NovaPoshtaWarehouse
        {
            public int CityId { get; set; }
            public int WareId { get; set; }
            public int Number { get; set; }
            public string City { get; set; }
            public string Address { get; set; }
        }

        public class NovaPoshtaStreet
        {
            public int RegionId { get; set; }
            public int CityId { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public string City { get; set; }

        }

        public async Task<(bool result, string city, string address)> GetNovaPoshtaCityAndAddressWarehouse(int warehouseId)
        {
            var workingLanguage = await _workContext.GetWorkingLanguageAsync();
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();

            string city;
            string address;

            NovaPoshtaWarehouse warehouse;
            if (workingLanguage.Id == UkrainianLanguageId)
            {
                warehouse =
                    (await _dataProvider.QueryAsync<NovaPoshtaWarehouse>(@"SELECT c.NameUa as City, AddressUa as Address, w.CityId as CityId
                        FROM [dbo].[NovaPoshta_Warehouse] w INNER JOIN [dbo].[NovaPoshta_City] c on w.CityId = c.CityId
                        WHERE w.WareId = " + warehouseId)).FirstOrDefault();
            }
            else
            {
                warehouse =
                    (await _dataProvider.QueryAsync<NovaPoshtaWarehouse>(@"SELECT c.NameRu as City, AddressRu as Address, w.CityId as CityId
                        FROM [dbo].[NovaPoshta_Warehouse] w INNER JOIN [dbo].[NovaPoshta_City] c on w.CityId = c.CityId
                        WHERE WareId = " + warehouseId)).FirstOrDefault();
            }

            if (warehouse == null)
            {
                city = address = string.Empty;

                return (false, city, address);
            }

            if (currentCustomer != null)
            {
                await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.NovaPoshtaCityId, warehouse.CityId);
            }

            city = warehouse.City;
            address = warehouse.Address;
            return (false, city, address);
        }

        public async Task<(bool result, string city, string address)> GetNovaPoshtaCityAndAddressStreet(int streetId)
        {
            var workingLanguage = await _workContext.GetWorkingLanguageAsync();
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();

            string city;
            string address;

            NovaPoshtaStreet street;
            if (workingLanguage.Id == UkrainianLanguageId)
            {
                street =
                    (await _dataProvider.QueryAsync<NovaPoshtaStreet>(@"SELECT c.NameUa as City, w.Name as Name, w.CityId as CityId
                        FROM [dbo].[NovaPoshta_Street] w INNER JOIN [dbo].[NovaPoshta_City] c on w.CityId = c.CityId
                        WHERE w.Id = " + streetId)).FirstOrDefault();
            }
            else
            {
                street =
                    (await _dataProvider.QueryAsync<NovaPoshtaStreet>(@"SELECT c.NameRu as City, w.Name as Name, w.CityId as CityId
                        FROM [dbo].[NovaPoshta_Street] w INNER JOIN [dbo].[NovaPoshta_City] c on w.CityId = c.CityId
                        WHERE w.Id = " + streetId)).FirstOrDefault();
            }

            if (street == null)
            {
                city = address = string.Empty;

                return (false, city, address);
            }

            if (currentCustomer != null)
            {
                await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.NovaPoshtaCityId, street.CityId);
                await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.NovaPoshtaStreetId, streetId);
            }

            city = street.City;
            address = street.Name;
            return (false, city, address);
        }
        //SAT
        public async Task<(bool result, string city, string address)> GetSATCityAndAddressWarehouse(int warehouseId)
        {
            var workingLanguage = await _workContext.GetWorkingLanguageAsync();
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();

            string city;
            string address;

            NovaPoshtaWarehouse warehouse;

            warehouse =
                (await _dataProvider.QueryAsync<NovaPoshtaWarehouse>(@"SELECT c.Name as City, Address as Address, w.CityId as CityId
                        FROM [dbo].[SAT_Warehouse] w INNER JOIN [dbo].[SAT_City] c on w.CityId = c.CityId
                        WHERE w.WareId = " + warehouseId)).FirstOrDefault();


            if (warehouse == null)
            {
                city = address = string.Empty;

                return (false, city, address);
            }

            city = warehouse.City;
            address = warehouse.Address;
            if (currentCustomer != null)
            {
                await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.SATCityId, warehouse.CityId);
            }
            return (false, city, address);
        }

        public async Task<(bool result, string city, string address)> GetSATCityAndAddressStreet(int streetId)
        {
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();

            string city;
            string address;

            var street =
                (await _dataProvider.QueryAsync<NovaPoshtaStreet>(@"
SELECT 
      S.Name as City
      ,N.Name as Name
      ,S.CityId as CityId
     FROM NovaPoshta_Street N 
        INNER JOIN [Sat_City] S ON S.[NovaPoshtaCityId]= N.cityId WHERE N.Id = @Id", new DataParameter("Id", streetId)))
                    .FirstOrDefault();

            if (street == null)
            {
                city = address = string.Empty;

                return (false, city, address);
            }

            if (currentCustomer != null)
            {
                await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.SATCityId, street.CityId);
                await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.SATStreetId, streetId);
            }

            city = street.City;
            address = street.Name;
            return (false, city, address);
        }

        //meest
        public async Task<(bool result, string city, string address)> GetMeestCityAndAddressWarehouse(int warehouseId)
        {
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();

            string city;
            string address;

            var warehouse =
                (await _dataProvider.QueryAsync<NovaPoshtaWarehouse>(@"SELECT c.NameUa as City, AddressUa as Address, w.CityId as CityId
                        FROM [dbo].[Meest_Warehouse] w INNER JOIN [dbo].[Meest_City] c on w.CityId = c.CityId
                        WHERE w.WareId = " + warehouseId)).FirstOrDefault();


            if (warehouse == null)
            {
                city = address = string.Empty;

                return (false, city, address);
            }

            if (currentCustomer != null)
            {
                await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.MeestCityId, warehouse.CityId);
            }

            city = warehouse.City;
            address = warehouse.Address;
            return (false, city, address);
        }

        public async Task<(bool result, string city, string address)> GetMeestCityAndAddressStreet(int streetId)
        {
            var workingLanguage = await _workContext.GetWorkingLanguageAsync();
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();

            string city;
            string address;

            var street =
                (await _dataProvider.QueryAsync<NovaPoshtaStreet>(@"SELECT c.regionId as RegionId, c.NameUa as City, w.Name as Name, w.CityId as CityId
                        FROM [dbo].[Meest_Street] w INNER JOIN [dbo].[Meest_City] c on w.CityId = c.CityId
                        WHERE w.Id = " + streetId)).FirstOrDefault();

            if (street == null)
            {
                city = address = string.Empty;
                return (false, city, address);
            }

            if (currentCustomer != null)
            {
                await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.MeestCityId, street.CityId);
                await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.MeestStreetId, streetId);
                await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.MeestRegionId, street.RegionId);
            }

            city = street.City;
            address = street.Name;
            return (false, city, address);
        }

        public async Task<(AddressType type, int weight)> GetNovaPoshtaStatus()
        {
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();
            var currentStore = await _storeContext.GetCurrentStoreAsync();

            var weight = 0;
            AddressType addressType;
            //validation

            var cart = await _shoppingCartService.GetShoppingCartAsync(currentCustomer, ShoppingCartType.ShoppingCart, currentStore.Id);

            var selectedShippingOption = await _genericAttributeService.GetAttributeAsync<ShippingOption>(currentCustomer, NopCustomerDefaults.SelectedShippingOptionAttribute, currentStore.Id);

            if (selectedShippingOption.Name.ToLower().Contains("склад") ||
                selectedShippingOption.Name.ToLower().Contains("ідділення") ||
            selectedShippingOption.Name.ToLower().Contains("тделение"))
            {
                //int maxLength = 0;
                //decimal totalWeight = 0;

                foreach (var item in cart)
                {
                    //totalWeight = totalWeight + Math.Max(item.Product.Weight, item.Product.VolumeWeight) * item.Quantity;
                    //maxLength = Math.Max(maxLength, item.Product.MaxLength);
                }

                //if (totalWeight > 1000)
                //{
                //    totalWeight = 999;
                //}
                //weight = (int)totalWeight;
                if (selectedShippingOption.Name.ToLower().Contains("нова"))
                {
                    addressType = AddressType.NovaPoshtaWarehouse;
                    //if (maxLength > 120)
                    //{
                    //    weight = (int)Math.Max(totalWeight, 31);
                    //}
                }
                else if (selectedShippingOption.Name.ToLower().Contains("кспр"))
                {
                    addressType = AddressType.MeestWarehouse;
                }
                else
                {
                    addressType = AddressType.SATWarehouse;
                }

                return (addressType, weight);
            }
            if (selectedShippingOption.Name.ToLower().Contains("нова"))
            {
                addressType = AddressType.NovaPoshtaAddress;
            }
            else
            if (selectedShippingOption.Name.ToLower().Contains("sat"))
            {
                addressType = AddressType.SATAddress;
            }
            else if (selectedShippingOption.Name.ToLower().Contains("кспр"))
            {
                addressType = AddressType.MeestAddress;
            }
            else
            {
                addressType = AddressType.Address;
            }

            return (addressType, weight);
        }


        #region NP related
        private async Task PrepareNovaPoshtaStreetInfoUa(CheckoutShippingAddressModel model)
        {
            var cities = await _dataProvider.QueryAsync<NovaPoshtaCity>(@"SELECT 
      [cityId] as Id, [nameUa] as Name
     FROM NovaPoshta_City ORDER BY [nameUa]");
            model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
            {
                Text = "Місто",
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaCityId == 0
            });
            foreach (var c in cities)
            {
                model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.ShippingNewAddress.NovaPoshtaCityId
                });
            }
            var warehouses = await _dataProvider.QueryAsync<NovaPoshtaStreet>(@"SELECT 
      [cityId] as CityId
      ,[Id] as Id
      ,[Name] as Name
     FROM NovaPoshta_Street WHERE cityId = " + model.ShippingNewAddress.NovaPoshtaCityId + " ORDER BY Name");

            model.ShippingNewAddress.NovaPoshtaStreet.Add(new SelectListItem()
            {
                Text = "Вулиця",
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaStreetId == 0
            });
            foreach (var c in warehouses)
            {
                model.ShippingNewAddress.NovaPoshtaStreet.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.ShippingNewAddress.NovaPoshtaStreetId
                });
            }
        }

        private async Task PrepareNovaPoshtaStreetInfoRu(CheckoutShippingAddressModel model)
        {
            var cities = await _dataProvider.QueryAsync<NovaPoshtaCity>(@"SELECT 
      [cityId] as Id, [nameRu] as Name
     FROM NovaPoshta_City ORDER BY [nameRu]");
            model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
            {
                Text = "Город",
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaCityId == 0
            });
            foreach (var c in cities)
            {
                model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.ShippingNewAddress.NovaPoshtaCityId
                });
            }
            var warehouses = await _dataProvider.QueryAsync<NovaPoshtaStreet>(@"SELECT 
      [cityId] as CityId
      ,[Id] as Id
      ,[Name] as Name
     FROM NovaPoshta_Street WHERE cityId = " + model.ShippingNewAddress.NovaPoshtaCityId + " ORDER BY Name");

            model.ShippingNewAddress.NovaPoshtaStreet.Add(new SelectListItem()
            {
                Text = "Вулиця",
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaStreetId == 0
            });
            foreach (var c in warehouses)
            {
                model.ShippingNewAddress.NovaPoshtaStreet.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.ShippingNewAddress.NovaPoshtaStreetId
                });
            }
        }

        private async Task PrepareSATInfo(CheckoutShippingAddressModel model)
        {
            var cities = await _dataProvider.QueryAsync<NovaPoshtaStreet>(@"SELECT 
      
      cityid as Id
      ,[Name] as Name
     FROM SAT_City ORDER BY Name");

            model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
            {
                Text = "Місто",
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaCityId == 0
            });
            foreach (var c in cities)
            {
                model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.ShippingNewAddress.NovaPoshtaCityId
                });
            }

            var warehouses = await _dataProvider.QueryAsync<NovaPoshtaStreet>(@"SELECT 
      S.[cityId] as CityId
      ,N.[Id] as Id
      ,N.[Name] as Name
     FROM NovaPoshta_Street N 
        INNER JOIN [Sat_City] S ON S.[NovaPoshtaCityId]= N.cityId WHERE S.cityId = " + model.ShippingNewAddress.NovaPoshtaCityId + " AND n.Name not like '% с.' AND n.Name not like '% с-ще.' AND n.Name not like '% смт.' ORDER BY N.Name");

            model.ShippingNewAddress.NovaPoshtaStreet.Add(new SelectListItem()
            {
                Text = "Вулиця",
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaStreetId == 0
            });
            foreach (var c in warehouses)
            {
                model.ShippingNewAddress.NovaPoshtaStreet.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.ShippingNewAddress.NovaPoshtaStreetId
                });
            }
        }

        private async Task PrepareMeestRegionInfo(CheckoutShippingAddressModel model)
        {
            var regions = await _dataProvider.QueryAsync<NovaPoshtaRegion>(@"
  SELECT [RegionId] as Id
      ,[nameUa] as Name
  FROM [Meest_Region]
  ORDER BY [nameUa]
");
            model.ShippingNewAddress.NovaPoshtaRegion.Add(new SelectListItem()
            {
                Text = "Область",
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaRegionId == 0
            });
            foreach (var c in regions)
            {
                model.ShippingNewAddress.NovaPoshtaRegion.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.ShippingNewAddress.NovaPoshtaRegionId
                });
            }

            var cities = await _dataProvider.QueryAsync<NovaPoshtaStreet>(@"SELECT 
      [RegionId] as RegionId
      ,[CityId] as Id
      ,[NameUA] as Name
     FROM Meest_City WHERE regionId = " + model.ShippingNewAddress.NovaPoshtaRegionId + " ORDER BY Name");

            model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
            {
                Text = "Місто",
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaCityId == 0
            });
            foreach (var c in cities)
            {
                model.ShippingNewAddress.NovaPoshtaCity.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.ShippingNewAddress.NovaPoshtaCityId
                });
            }

            var warehouses = await _dataProvider.QueryAsync<NovaPoshtaStreet>(@"SELECT 
      [cityId] as CityId
      ,[Id] as Id
      ,[Name] as Name
     FROM Meest_Street WHERE cityId = " + model.ShippingNewAddress.NovaPoshtaCityId + " ORDER BY Name");

            model.ShippingNewAddress.NovaPoshtaStreet.Add(new SelectListItem()
            {
                Text = "Вулиця",
                Value = "0",
                Selected = model.ShippingNewAddress.NovaPoshtaStreetId == 0
            });
            foreach (var c in warehouses)
            {
                model.ShippingNewAddress.NovaPoshtaStreet.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.ShippingNewAddress.NovaPoshtaStreetId
                });
            }
        }

        #endregion

    }
}