using Microsoft.AspNetCore.Mvc;
using ML.CMS.Models;
using ML.CMS.Configuration;
using ML.CMS.Helpers;
using Smartstore.ComponentModel;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Core.Checkout.Orders;
using System.Xml;
using System.IO;
using System;
using Smartstore.IO;
using static Smartstore.Admin.Models.Export.ExportFileDetailsModel;
using Smartstore.Engine.Modularity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using System.Xml.Linq;
using Smartstore;
using ML.CMS.Helper;
using ML.CMS.Services;
using Smartstore.Core.Data;
using Microsoft.IdentityModel.Tokens;
using Smartstore.Core.Logging;
using Smartstore.Core.DataExchange;
using Smartstore.Core.Localization;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using static Smartstore.Core.Security.Permissions.Configuration;
using Language = Smartstore.Core.Localization.Language;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace ML.CMS.Controllers
{

    [Area("Admin")]
    public class CMSController : ModuleController
    {
        private readonly SmartDbContext _db;
        private readonly ILanguageService _languageService;
        private readonly CMSXMLFileService _CMSXMLFileService;
        private readonly List<Language> _languages;

        //public ILogger Logger { get; set; } = NullLogger.Instance;

        public CMSController(SmartDbContext db, ILanguageService languageService, CMSXMLFileService CMSXMLFileService)
        {
            _db = db;
            _languageService = languageService;
            _CMSXMLFileService = CMSXMLFileService;
            _languages = _languageService.GetAllLanguages();
        }



        [AuthorizeAdmin, Permission(CMSPermissions.Read)]
        [LoadSetting]
        public IActionResult Configure(CMSSettings settings)
        {
            var model = MiniMapper.Map<CMSSettings, ConfigurationModel>(settings);
            return View(model);
        }


        [AuthorizeAdmin, Permission(CMSPermissions.Update)]
        [HttpPost]
        public async Task<IActionResult> CreateBak()
        {
            return null;
        }

        [AuthorizeAdmin, Permission(CMSPermissions.Update)]
        [HttpPost]
        public async Task<IActionResult> GetLanguageResource()
        {
            var success = false;
            dynamic message = string.Empty;
            dynamic data;
            dynamic jsonData;
            string[] idValues = Array.Empty<string>();

            try
            {
                data = await DeserializeJsonFromRequest(Request);
                jsonData = JObject.Parse(data);
                if (jsonData.id != null && jsonData.id is JArray idArray)
                {
                    idValues = jsonData.id.ToObject<string[]>();
                }

            }
            catch (JsonReaderException ex)
            {

                message = ("Invalid JSON data: " + ex.Message);
            }

            List<dynamic> collectMessage = new List<dynamic>();
            foreach (string idValue in idValues)
            {
                if (string.IsNullOrEmpty(idValue)) { message = "Missing ID"; }
                else
                {
                    XDocument enResource = await _CMSXMLFileService.LoadsXmlAsync("resources.en-us.xml");
                    XMLDocHelper enHelper = new XMLDocHelper(enResource);
                    enHelper.FlattenResourceFile();
                    string enValue = enHelper.GetValue(idValue) ?? "Not found";

                    XDocument deResource = await _CMSXMLFileService.LoadsXmlAsync("resources.de-de.xml");
                    XMLDocHelper deHelper = new XMLDocHelper(deResource);
                    string deValue = deHelper.GetValue(idValue) ?? "Not found";

                    collectMessage.Add(Json(new { ID = idValue, EN = enValue, DE = deValue }));
                    success = true;
                }
            }
            if (collectMessage.Count > 0)
            {
                message = collectMessage;
            }

            return Json(new
            {
                Success = success,
                Message = message
            });
        }

        private async Task<int> ImportResourcesFromXmlAsync(
        Language language,
        XDocument xmlDocument,
        bool sourceIsPlugin = true)
        {
            Guard.NotNull(language);
            Guard.NotNull(xmlDocument);

            List<object> activityLog = new List<object>();


            await _db.LoadCollectionAsync(language, x => x.LocaleStringResources);
            var resources = language.LocaleStringResources.ToDictionarySafe(x => x.ResourceName, StringComparer.OrdinalIgnoreCase);

            if (xmlDocument.Root == null || (xmlDocument.Descendants("Children").Count() > 0))
                return -1;

            var nodes = xmlDocument.Descendants("LocaleResource");
            var isDirty = false;

            foreach (var xel in nodes)
            {
                string name = (string)xel.Attribute("Name");
                name = name?.TrimSafe() ?? string.Empty;
                string value = (string)xel.Element("Value");

                if (string.IsNullOrEmpty(name))
                    continue;

                if (resources.TryGetValue(name, out var resource))
                {
                    if (value != resource.ResourceValue)
                    {
                        resource.ResourceValue = value;
                        resource.IsTouched = null;
                        resource.IsFromPlugin = sourceIsPlugin;
                        isDirty = true;
                        _db.Entry(resource).State = EntityState.Modified;
                        activityLog.Add(resource);
                    }
                }
                else
                {
                    var newResource = new LocaleStringResource
                    {
                        LanguageId = language.Id,
                        ResourceName = name,
                        ResourceValue = value,
                        IsFromPlugin = sourceIsPlugin
                    };

                    _db.LocaleStringResources.Add(newResource);
                    resources[name] = newResource;
                    activityLog.Add(resources[name]);
                    isDirty = true;
                }
            }

            if (isDirty)
            {
                string activityLogJson = JsonConvert.SerializeObject(activityLog);
                Logger.Info("ImportResourcesFromXmlAsync updated: {Json}.", activityLogJson);
                return await _db.SaveChangesAsync();
            }
            return 0;
        }

        [AuthorizeAdmin, Permission(CMSPermissions.Update)]
        [HttpPost]
        public async Task<IActionResult> UpdateResource()
        {
            var success = false;
            var message = string.Empty;

            JObject jsonData = await getRequestJSON();

            Logger.Info("UpdateResource called with request: {RequestBody}", JsonConvert.SerializeObject(jsonData));

            if (!jsonData.ContainsKey("ID") || string.IsNullOrEmpty(jsonData["ID"].ToString()))
            {
                return Json(new{Success = false,Message = "Invalid ID"});
            }

            string ID = jsonData["ID"].ToString();
            Dictionary<Language, string> updatedLanguageValues = _languages
                .Where(language => jsonData.ContainsKey(language.LanguageCulture))
                .ToDictionary(
                    language => language,
                    language => jsonData[language.LanguageCulture].ToString()
                );

            foreach (var updatedValue in updatedLanguageValues)
            {
                string languageCulture = updatedValue.Key.LanguageCulture;
                try
                {
                    XDocument xResource = await _CMSXMLFileService.LoadsXmlAsync($"resources.{languageCulture}.xml");
                    XMLDocHelper xHelper = cleanXDoc(ID, updatedValue.Value, xResource);
                    await _CMSXMLFileService.SaveXmlAsync(xHelper.Content, $"resources.{languageCulture}.xml");
                    await ImportResourcesFromXmlAsync(updatedValue.Key, xHelper.Content);
                    message += $"Successful updated {languageCulture}; ";
                    success = true;
                }
                catch (Exception ex)
                {
                    message += $"Error updating {languageCulture}: {ex.Message}; ";
                    Logger.Error("UpdateResource failed: {ex.Message}.", ex.Message, ex);
                }

            }

            return Json(new
            {
                Success = success,
                Message = message
            });
        }

        private async Task<JObject> getRequestJSON()
        {
            dynamic data = await DeserializeJsonFromRequest(Request);
            JObject jsonData = JsonConvert.DeserializeObject(data);
            return jsonData;
        }

        private XMLDocHelper cleanXDoc(string ID, string value, XDocument xResource)
        {
            XMLDocHelper xHelper = new XMLDocHelper(xResource);
            xHelper.FlattenResourceFile();
            xHelper.SetAppendRoot();
            xHelper.ChangeValue(ID, value);
            xHelper.sortElements();
            //var duplicates = xHelper.GetDuplicates();
            //if (duplicates.Count > 0)
            //{
            //    message += $"Duplicates found: {string.Join(",", duplicates)}; ";
            //}
            return xHelper;
        }

        [AuthorizeAdmin, Permission(CMSPermissions.Update)]
        [HttpPost, SaveSetting]
        public IActionResult Configure(ConfigurationModel model, CMSSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return Configure(settings);
            }

            ModelState.Clear();
            MiniMapper.Map(model, settings);

            return RedirectToAction(nameof(Configure));
        }

        [AuthorizeAdmin]
        public IActionResult ProductEditTab(int productId)
        {
            var model = new BackendExtensionModel
            {
                Welcome = "Hello world!"
            };

            ViewData.TemplateInfo.HtmlFieldPrefix = "CustomProperties[CMS]";
            return View(model);
        }

        public async Task<dynamic> DeserializeJsonFromRequest(HttpRequest request)
        {
            var requestBody = string.Empty;
            try
            {
                using (var reader = new StreamReader(request.Body))
                {
                    requestBody = await reader.ReadToEndAsync();
                }
                return requestBody;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while deserializing JSON from the request body", ex);
            }

        }
    }
}


///IXmlResourceManagerExtensions
/////Imports language resources from XML file. This method commits to db.
//////ImportResourcesFromXmlAsync
///
//
//_activityLogger.LogActivity(KnownActivityLogTypes.AddNewCheckoutAttribute, T("ActivityLog.AddNewCheckoutAttribute"), checkoutAttribute.Name);

//NotifySuccess(T("Admin.Catalog.Attributes.CheckoutAttributes.Added"));