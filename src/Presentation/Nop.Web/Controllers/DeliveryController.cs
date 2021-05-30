using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB.Data;
using MeestExpressApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Security;
using Nop.Core.Events;
using Nop.Core.Rss;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;
using NovaPoshtaApi;
using SatApi;
using SatApi.RequestModels;

namespace Nop.Web.Controllers
{
    public class DeliveryController : BasePublicController
    {
        #region Fields

        private static NovaPoshtaApi.NovaPoshtaApi novaPoshtaHelper = new NovaPoshtaApi.NovaPoshtaApi(ConfigurationManager.AppSettings["key"]);
        private readonly CaptchaSettings _captchaSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly IAclService _aclService;
        private readonly ICompareProductsService _compareProductsService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerService _customerService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderService _orderService;
        private readonly IPermissionService _permissionService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductModelFactory _productModelFactory;
        private readonly IProductService _productService;
        private readonly IRecentlyViewedProductsService _recentlyViewedProductsService;
        private readonly IReviewTypeService _reviewTypeService;
        private readonly IShoppingCartModelFactory _shoppingCartModelFactory;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly LocalizationSettings _localizationSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly INopDataProvider _dataProvider;
        private readonly IGenericAttributeService _genericAttributeService;

        #endregion

        #region Ctor

        public DeliveryController(CaptchaSettings captchaSettings,
            CatalogSettings catalogSettings,
            IAclService aclService,
            ICompareProductsService compareProductsService,
            ICustomerActivityService customerActivityService,
            ICustomerService customerService,
            IEventPublisher eventPublisher,
            ILocalizationService localizationService,
            IOrderService orderService,
            IPermissionService permissionService,
            IProductAttributeParser productAttributeParser,
            IProductModelFactory productModelFactory,
            IProductService productService,
            IRecentlyViewedProductsService recentlyViewedProductsService,
            IReviewTypeService reviewTypeService,
            IShoppingCartModelFactory shoppingCartModelFactory,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            IUrlRecordService urlRecordService,
            IWebHelper webHelper,
            IWorkContext workContext,
            IWorkflowMessageService workflowMessageService,
            LocalizationSettings localizationSettings,
            ShoppingCartSettings shoppingCartSettings,
            INopDataProvider dataProvider,
            IGenericAttributeService genericAttributeService)
        {
            _captchaSettings = captchaSettings;
            _catalogSettings = catalogSettings;
            _aclService = aclService;
            _compareProductsService = compareProductsService;
            _customerActivityService = customerActivityService;
            _customerService = customerService;
            _eventPublisher = eventPublisher;
            _localizationService = localizationService;
            _orderService = orderService;
            _permissionService = permissionService;
            _productAttributeParser = productAttributeParser;
            _productModelFactory = productModelFactory;
            _productService = productService;
            _reviewTypeService = reviewTypeService;
            _recentlyViewedProductsService = recentlyViewedProductsService;
            _shoppingCartModelFactory = shoppingCartModelFactory;
            _shoppingCartService = shoppingCartService;
            _storeContext = storeContext;
            _storeMappingService = storeMappingService;
            _urlRecordService = urlRecordService;
            _webHelper = webHelper;
            _workContext = workContext;
            _workflowMessageService = workflowMessageService;
            _localizationSettings = localizationSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _dataProvider = dataProvider;
            _genericAttributeService = genericAttributeService;
        }

        #endregion


        private async Task PrepareDeliveryProceModel(DeliveryPriceModel model, int productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || product.Deleted)
                throw new ArgumentException("No product found with the specified SKU");

            model.ProductId = productId;
            model.ProductName = await _localizationService.GetLocalizedAsync(product, x => x.Name);
            model.ProductShortDescription = await _localizationService.GetLocalizedAsync(product, x => x.ShortDescription);
            model.ProductSeName = await _urlRecordService.GetSeNameAsync(product.Name, false, false);// product.GetSeName();
            model.ProductPrice = (double)product.Price;
            model.Weight = (double)product.Weight;
            model.VolumeWeight = (double)product.VolumeWeight;

            model.IsObr = (await _dataProvider.QueryAsync<int?>(@" SELECT Max(
  case when IsObr = 1 then 2 when IsObr = 0 then 0 else 1 end
    ) 
  from ProductParts pp INNER JOIN ProductPart p on pp.ArticleNumber = p.ArticleNumber
  where pp.ManufacturerPartNumber = @MP", new DataParameter("MP", product.ManufacturerPartNumber))).FirstOrDefault() ?? 0;
        }

        private async Task PopulateNovaposhtaDelivery(DeliveryPriceModel model)
        {
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();
            var workingLanguage = await _workContext.GetWorkingLanguageAsync();

            if (currentCustomer != null)
            {
                if (model.CityId != 0)
                {
                    await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.NovaPoshtaCityId, model.CityId);
                }
                else
                {
                    model.CityId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.NovaPoshtaCityId);
                }
            }

            var isUkrainianLanguage = workingLanguage.Id == CheckoutModelFactory.UkrainianLanguageId;
            var cityNameField = isUkrainianLanguage ? "nameUa" : "nameRu";

            var cities = (await _dataProvider.QueryAsync<CityModel>($@"SELECT 
      [cityId] as Id, [{cityNameField}] as Name
     FROM NovaPoshta_City ORDER BY [{cityNameField}]")).ToList();

            model.Cities = new List<SelectListItem>{
                new SelectListItem()
                {
                    Text = isUkrainianLanguage ? "Місто" : "Город",
                    Value = "0",
                    Selected = model.CityId == 0
                }
            };

            foreach (var c in cities)
            {
                model.Cities.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.CityId
                });
            }
        }

        private async Task PopulateMeestDelivery(DeliveryPriceModel model)
        {
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();
            var workingLanguage = await _workContext.GetWorkingLanguageAsync();

            if (currentCustomer != null)
            {
                if (model.CityId != 0)
                {
                    await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.MeestCityId, model.CityId);
                }
                else
                {
                    model.CityId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer, NopCustomerDefaults.MeestCityId);
                }
            }

            var isUkrainianLanguage = workingLanguage.Id == CheckoutModelFactory.UkrainianLanguageId;
            var cityNameField = isUkrainianLanguage ? "nameUa" : "nameRu";

            var cities = (await _dataProvider.QueryAsync<CityModel>($"SELECT CityId as Id, {cityNameField} as Name FROM Meest_City " +
                $"WHERE cityid IN (SELECT DISTINCT cityid FROM Meest_Warehouse WHERE max_weight_allowed = 1000) " +
                $"ORDER BY [{cityNameField}]")).ToList();

            model.Cities = new List<SelectListItem>{
                new SelectListItem()
                {
                    Text = isUkrainianLanguage ? "Місто" : "Город",
                    Value = "0",
                    Selected = model.CityId == 0
                }
            };

            foreach (var c in cities)
            {
                model.Cities.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.CityId
                });
            }
        }

        private async Task PopulateSatDelivery(DeliveryPriceModel model)
        {
            var currentCustomer = await _workContext.GetCurrentCustomerAsync();
            var workingLanguage = await _workContext.GetWorkingLanguageAsync();

            if (currentCustomer != null)
            {
                if (model.CityId != 0)
                {
                    await _genericAttributeService.SaveAttributeAsync(currentCustomer, NopCustomerDefaults.SATCityId, model.CityId);
                }
                else
                {
                    model.CityId = await _genericAttributeService.GetAttributeAsync<int>(currentCustomer,NopCustomerDefaults.SATCityId);
                }
            }

            var isUkrainianLanguage = workingLanguage.Id == CheckoutModelFactory.UkrainianLanguageId;

            var cities =  (await _dataProvider.QueryAsync<CityModel>($"SELECT CityId as Id, Name FROM Sat_City ORDER BY Name")).ToList();

            model.Cities = new List<SelectListItem>{
                new SelectListItem()
                {
                    Text = isUkrainianLanguage ? "Місто" : "Город",
                    Value = "0",
                    Selected = model.CityId == 0
                }
            };

            foreach (var c in cities)
            {
                model.Cities.Add(new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.CityId
                });
            }
        }

        //  [HttpPost, ActionName("DeliveryPricePopup")]
        public async Task<ActionResult> DeliveryPricePopup(int productId, string cityId, string deliveryType)
        {
            var model = new DeliveryPriceModel { DeliveryType = deliveryType };
            await PrepareDeliveryProceModel(model, productId);

            if (int.TryParse(cityId, out int id))
            {
                model.CityId = id;
            }

            switch (deliveryType)
            {
                case "meest":
                    await PopulateMeestDelivery(model);
                    await CalculateMeestDeliveryPrice(model);
                    break;
                case "sat":
                    await PopulateSatDelivery(model);
                    await CalculateSatDeliveryPrice(model);
                    break;
                case "novaposhta":
                default:
                    await PopulateNovaposhtaDelivery(model);
                    await CalculateNovaposhtaDeliveryPrice(model);
                    break;
            }

            return View(model);
        }

        private async Task CalculateNovaposhtaDeliveryPrice(DeliveryPriceModel model)
        {
            var weight = Math.Max(model.Weight, model.VolumeWeight);
            if (weight == 0 || model.CityId == 0)
            {
                return;
            }

            var refer = (await   _dataProvider.QueryAsync<string>(@"SELECT Ref
                   FROM NovaPoshta_City WHERE [cityId] = @CityId", new DataParameter("CityId", model.CityId))).First();

            var helper = new NovaPoshtaApi.InternetDocumentHelper(novaPoshtaHelper);
            model.DeliveryPrice =
                helper.GetDocumentPrice(refer, weight, "WarehouseWarehouse",
                    (int)Math.Round(model.DeliveryPrice), "db5c88f5-391c-11dd-90d9-001a92567626").Data[0].Cost;
            model.DeliveryPriceHome =
                helper.GetDocumentPrice(refer, weight, "WarehouseDoors",
                     (int)Math.Round(model.DeliveryPrice), "db5c88f5-391c-11dd-90d9-001a92567626").Data[0].Cost;

            //if (model.ProductPrice > 300)
            //{
            //    model.DeliveryPrice = model.DeliveryPrice + model.ProductPrice*0.005;
            //    model.DeliveryPriceHome = model.DeliveryPriceHome + model.ProductPrice*0.005;
            //}


            if (model.IsObr == 2)
            {
                model.DeliveryPrice = model.DeliveryPrice + Math.Max(model.VolumeWeight / 250, 0.5) * 220;
                model.DeliveryPriceHome = model.DeliveryPriceHome + Math.Max(model.VolumeWeight / 250, 0.5) * 220;
            }

            model.DeliveryPrice = Math.Round(model.DeliveryPrice);
            model.DeliveryPriceHome = Math.Round(model.DeliveryPriceHome);
        }

        private async Task CalculateMeestDeliveryPrice(DeliveryPriceModel model)
        {
            var weight = Math.Max(model.Weight, model.VolumeWeight);
            if (weight == 0 || model.CityId == 0)
            {
                return;
            }

            var client = new MeestExpressApiHelper();

            var cityRef = (await _dataProvider.QueryAsync<string>(@"SELECT Ref FROM Meest_City WHERE [cityId] = @CityId", new DataParameter("CityId", model.CityId))).First();
            var branches = client.Branch($"CityUUID = '{cityRef}'");

            // Take first branch with no weight limits
            var branch = branches.Where(x => x.Branchtype == "ППВ").FirstOrDefault();

            if (branch != null)
            {
                model.DeliveryPrice = Math.Round(client.CalculateShipments(branch.CityRef, null, true, 1, weight, 1, 1, 1, model.ProductPrice));
                model.DeliveryPriceHome = Math.Round(client.CalculateShipments(null, branch.Ref, false, 1, weight, 1, 1, 1, model.ProductPrice));
            }
        }

        private async Task CalculateSatDeliveryPrice(DeliveryPriceModel model)
        {
            var weight = Math.Max(model.Weight, model.VolumeWeight);
            if (weight == 0 || model.CityId == 0)
            {
                return;
            }

            var client = new SatApi.SatApi(SatDelivaryApiKey);

            var cityRef =  (await _dataProvider.QueryAsync<string>(@"SELECT Ref FROM Sat_City WHERE [cityId] = @CityId", new DataParameter("CityId", model.CityId))).First();
            var city = client.GetCity(cityRef);
            var response = client.CalcDelivery(new CalcDeliveryRequest
            {
                RspSender = "b5a47694-385d-11dd-a17c-001a4d3b885e", // Lviv
                RspRecipient = city.Data[0].RspRef,
                Weight = weight,
                CargoType = SatApi.InternetDocumentHelper.GetCargoType(weight)
            });

            model.DeliveryPrice =
            model.DeliveryPriceHome = Math.Round(response.Data[0].Cost);
        }

        public class CityModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private const string SatDelivaryApiKey = "129991e7-a924-4bda-8a07-3239f051";
    }
}
