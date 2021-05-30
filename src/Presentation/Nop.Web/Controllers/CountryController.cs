using LinqToDB.Data;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Data;
using Nop.Services.Localization;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Web.Controllers
{
    public partial class CountryController : BasePublicController
    {
        #region Fields

        private readonly ICountryModelFactory _countryModelFactory;
        private readonly INopDataProvider _dataProvider;
        private readonly IWorkContext _workContext;
        private readonly IStaticCacheManager _cacheManager;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public CountryController(ICountryModelFactory countryModelFactory, INopDataProvider dataProvider, IWorkContext workContext, IStaticCacheManager cacheManager, ILocalizationService localizationService)
        {
            _countryModelFactory = countryModelFactory;
            _dataProvider = dataProvider;
            _workContext = workContext;
            _cacheManager = cacheManager;
            _localizationService = localizationService;
        }

        #endregion

        #region States / provinces

        //available even when navigation is not allowed
        [CheckAccessPublicStore(true)]
        //ignore SEO friendly URLs checks
        [CheckLanguageSeoCode(true)]
        public virtual async Task<IActionResult> GetStatesByCountryId(string countryId, bool addSelectStateItem)
        {
            var model = await _countryModelFactory.GetStatesByCountryIdAsync(countryId, addSelectStateItem);

            return Json(model);
        }

        #endregion

        [HttpGet]
        public async Task<ActionResult> GetNovaPoshtaWarehousesByCityId(string cityId, int weightLimitation)
        {
            var workingLanguage = await _workContext.GetWorkingLanguageAsync();

            //this action method gets called via an ajax request
            if (string.IsNullOrEmpty(cityId))
                throw new ArgumentNullException("cityId");

            if (weightLimitation > 1000)
            {
                weightLimitation = 999;
            }

            //bool weightLimitationB = weightLimitation == "1";

            var cacheKey = new CacheKey(string.Format("Nop.NovaPoshtaWarehousesByCountry-{0}-{1}-{2}", cityId, workingLanguage.Id, weightLimitation));
            var cacheModel = _cacheManager.GetAsync(cacheKey, async () =>
            {
                List<CheckoutModelFactory.NovaPoshtaWarehouse> warehouses;
                if (workingLanguage.Id == CheckoutModelFactory.UkrainianLanguageId)
                {
                    warehouses = (await _dataProvider.QueryAsync<CheckoutModelFactory.NovaPoshtaWarehouse>(@"SELECT 
      [cityId] as CityId
      ,[wareId] as WareId
     ,[addressUa] as Address
     FROM NovaPoshta_Warehouse WHERE cityId =  @cityId " +
       " AND max_weight_allowed > " + weightLimitation +
     " ORDER BY Number", new DataParameter("cityId", cityId))).ToList();
                }
                else
                {
                    warehouses = (await _dataProvider.QueryAsync<CheckoutModelFactory.NovaPoshtaWarehouse>(@"SELECT 
      [cityId] as CityId
      ,[wareId] as WareId
     ,[addressRu] as Address
     FROM NovaPoshta_Warehouse WHERE cityId =  @cityId " +
       " AND max_weight_allowed > " + weightLimitation +
     " ORDER BY Number", new DataParameter("cityId", cityId))).ToList();
                }

                var result = (from s in warehouses
                              select new { id = s.WareId, name = s.Address })
                    .ToList();
                result.Insert(0, new { id = 0, name = await _localizationService.GetResourceAsync("Custom.Warehouse") });
                return result;
            });
            return Json(cacheModel);
        }

        [HttpGet]
        public async Task<ActionResult> GetNovaPoshtaStreetByCityId(string cityId)
        {
            //this action method gets called via an ajax request
            if (string.IsNullOrEmpty(cityId))
                throw new ArgumentNullException("cityId");
            var cacheKey = new CacheKey(string.Format("Nop.NovaPoshtaStreetByCountry-{0}", cityId));
            var cacheModel = _cacheManager.Get(cacheKey, async () =>
            {
                var streets = (await _dataProvider.QueryAsync<CheckoutModelFactory.NovaPoshtaStreet>(@"SELECT 
      [cityId] as CityId
      ,[Id] as Id
     ,[Name] as Name
     FROM NovaPoshta_Street WHERE cityId = @cityId ORDER BY Name",
                    new DataParameter("cityId", cityId))).ToList();
                var result = (from s in streets
                              select new { id = s.Id, name = s.Name })
                    .ToList();
                result.Insert(0,
                    new { id = 0, name = await _localizationService.GetResourceAsync("Address.Fields.Street") });
                return result;
            });
            return Json(cacheModel);
        }

        [HttpGet]
        public async Task<ActionResult> GetSATWarehousesByCityId(string cityId)
        {
            var workingLanguage = await _workContext.GetWorkingLanguageAsync();

            //this action method gets called via an ajax request
            if (String.IsNullOrEmpty(cityId))
                throw new ArgumentNullException("cityId");

            var cacheKey = new CacheKey(string.Format("Nop.SATWarehousesByCountry-{0}-{1}", cityId, workingLanguage.Id));
            var cacheModel = _cacheManager.Get(cacheKey, async () =>
            {
                List<CheckoutModelFactory.NovaPoshtaWarehouse> warehouses;

                var query = @"SELECT 
                            [cityId] as CityId,
                            [wareId] as WareId,
                            [address] as Address
                            FROM SAT_Warehouse WHERE cityId =  @cityId ORDER BY Number";
                warehouses = (await _dataProvider.QueryAsync<CheckoutModelFactory.NovaPoshtaWarehouse>(query, new DataParameter("cityId", cityId))).ToList();


                var result = (from s in warehouses
                              select new { id = s.WareId, name = s.Address })
                    .ToList();
                result.Insert(0,
                    new { id = 0, name = await _localizationService.GetResourceAsync("Custom.Warehouse") });
                return result;
            });
            return Json(cacheModel);
        }

        [HttpGet]
        public async Task<ActionResult> GetSATStreetByCityId(string cityId)
        {
            //this action method gets called via an ajax request
            if (String.IsNullOrEmpty(cityId))
                throw new ArgumentNullException("cityId");
            var cacheKey = new CacheKey(string.Format("Nop.SATStreetByCountry-{0}", cityId));
            var cacheModel = _cacheManager.Get(cacheKey, async () =>
            {
                List<CheckoutModelFactory.NovaPoshtaStreet> streets;

                streets = (await _dataProvider.QueryAsync<CheckoutModelFactory.NovaPoshtaStreet>(@"SELECT 
      S.[cityId] as CityId
      ,N.[Id] as Id
      ,N.[Name] as Name
     FROM NovaPoshta_Street N 
        INNER JOIN [Sat_City] S ON S.[NovaPoshtaCityId]= N.cityId WHERE S.cityId = @cityId AND n.Name not like '% ñ.' AND n.Name not like '% ñ-ùå.' AND n.Name not like '% ñìò.' ORDER BY N.Name",
                    new DataParameter("cityId", cityId))).ToList();
                var result = (from s in streets
                              select new { id = s.Id, name = s.Name })
                    .ToList();
                result.Insert(0,
                    new { id = 0, name = await _localizationService.GetResourceAsync("Address.Fields.Street") });
                return result;
            });
            return Json(cacheModel);
        }

        ////Meest
        [HttpGet]
        public async Task<ActionResult> GetMeestWarehousesByCityId(string cityId, int weightLimitation)
        {
            var workingLanguage = await _workContext.GetWorkingLanguageAsync();

            //this action method gets called via an ajax request
            if (String.IsNullOrEmpty(cityId))
                throw new ArgumentNullException("cityId");

            if (weightLimitation > 1000)
            {
                weightLimitation = 999;
            }

            //bool weightLimitationB = weightLimitation == "1";

            var cacheKey = new CacheKey(string.Format("Nop.MeestWarehousesByCountry-{0}-{1}-{2}", cityId, workingLanguage.Id, weightLimitation));
            var cacheModel = _cacheManager.Get(cacheKey, async () =>
            {
                List<CheckoutModelFactory.NovaPoshtaWarehouse> warehouses;

                warehouses = (await _dataProvider.QueryAsync<CheckoutModelFactory.NovaPoshtaWarehouse>(@"SELECT 
      [cityId] as CityId
      ,[wareId] as WareId
     ,[addressUa] as Address
     FROM Meest_Warehouse WHERE cityId =  @cityId AND max_weight_allowed > " +
                                                                                          weightLimitation +
                                                                                          " ORDER BY [addressUa]",
                    new DataParameter("cityId", cityId))).ToList();


                var result = (from s in warehouses
                              select new { id = s.WareId, name = s.Address })
                    .ToList();
                result.Insert(0,
                    new { id = 0, name =await  _localizationService.GetResourceAsync("Custom.Warehouse") });
                return result;
            });
            return Json(cacheModel);
        }


        [HttpGet]
        public async Task<ActionResult> GetMeestStreetByCityId(string cityId)
        {
            //this action method gets called via an ajax request
            if (String.IsNullOrEmpty(cityId))
                throw new ArgumentNullException("cityId");
            var cacheKey = new CacheKey(string.Format("Nop.MeestStreetByCountry-{0}", cityId));
            var cacheModel = _cacheManager.Get(cacheKey, async () =>
            {
                List<CheckoutModelFactory.NovaPoshtaStreet> streets;

                streets =  (await _dataProvider.QueryAsync<CheckoutModelFactory.NovaPoshtaStreet>(@"SELECT 
      [cityId] as CityId
      ,[Id] as Id
     ,[Name] as Name
     FROM Meest_Street WHERE cityId = @cityId ORDER BY Name", new DataParameter("cityId", cityId))).ToList();
                var result = (from s in streets
                              select new { id = s.Id, name = s.Name })
                    .ToList();
                result.Insert(0,
                    new { id = 0, name =await  _localizationService.GetResourceAsync("Address.Fields.Street") });
                return result;
            });
            return Json(cacheModel);
        }

        [HttpGet]
        public async Task<ActionResult> GetMeestCityByRegionId(string regionId)
        {
            //this action method gets called via an ajax request
            if (String.IsNullOrEmpty(regionId))
                throw new ArgumentNullException("regionId");
            var cacheKey = new CacheKey(string.Format("Nop.MeestCityByRegionId-{0}", regionId));
            var cacheModel = _cacheManager.Get(cacheKey, async () =>
            {
                List<CheckoutModelFactory.NovaPoshtaCity> streets;

                streets =(await _dataProvider.QueryAsync<CheckoutModelFactory.NovaPoshtaCity>(@"SELECT 
      [regionId] as regionId
      ,[CityId] as Id
     ,[NameUa] as Name
     FROM Meest_City WHERE regionId = @regionId ORDER BY Name",
                    new DataParameter("regionId", regionId))).ToList();
                var result = (from s in streets
                              select new { id = s.Id, name = s.Name })
                    .ToList();
                result.Insert(0,
                    new { id = 0, name =await  _localizationService.GetResourceAsync("Address.Fields.City") });
                return result;
            });
            return Json(cacheModel);
        }


    }
}