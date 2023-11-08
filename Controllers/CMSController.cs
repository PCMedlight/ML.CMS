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

namespace ML.CMS.Controllers
{

    [Area("Admin")]
    public class CMSController : ModuleController
    {
        private readonly SmartDbContext _db;
        private readonly ILanguageService _languageService;
        private readonly CMSXMLFileService _CMSXMLFileService;

        public CMSController(SmartDbContext db, ILanguageService languageService, CMSXMLFileService CMSXMLFileService)
        {
            _db = db;
            _languageService = languageService;
            _CMSXMLFileService = CMSXMLFileService;
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
            string[] idValues = new string[0];

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

        [AuthorizeAdmin, Permission(CMSPermissions.Update)]
        private async Task<int> ImportResourcesFromXmlAsync(
        Language language,
        XDocument xmlDocument,
        string rootKey = null,
        bool sourceIsPlugin = true,
        ImportModeFlags mode = ImportModeFlags.Insert | ImportModeFlags.Update,
        bool updateTouchedResources = false)
        {
            Guard.NotNull(language);
            Guard.NotNull(xmlDocument);

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

                if (rootKey.HasValue())
                {
                    var appendRootKeyAttribute = xel.Attribute("AppendRootKey");
                    if (appendRootKeyAttribute == null || !appendRootKeyAttribute.Value.EqualsNoCase("false"))
                    {
                        name = string.Format("{0}.{1}", rootKey, name);
                    }
                }

                if (resources.TryGetValue(name, out var resource))
                {
                    if (mode.HasFlag(ImportModeFlags.Update))
                    {
                        if (updateTouchedResources || !resource.IsTouched.GetValueOrDefault())
                        {
                            if (value != resource.ResourceValue)
                            {
                                resource.ResourceValue = value;
                                resource.IsTouched = null;
                                isDirty = true;
                            }
                        }
                    }
                }
                else
                {
                    if (mode.HasFlag(ImportModeFlags.Insert))
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
                        isDirty = true;
                    }
                }
            }

            if (isDirty)
            {
                //_db.Entry(language).Collection(x => x.LocaleStringResources).Load();
                return await _db.SaveChangesAsync();
            }

            return 0;
        }

        [AuthorizeAdmin, Permission(CMSPermissions.Update)]
        [HttpPost]
        public async Task<IActionResult> UpdateResource(ConfigurationModel model, CMSSettings settings)
        {
            var success = false;
            var message = string.Empty;
            dynamic data;
            dynamic jsonData;
            string? ID = null;
            string? EN = null;
            string? DE = null;

            data = await DeserializeJsonFromRequest(Request);
            jsonData = JObject.Parse(data);
            if (jsonData.ContainsKey("ID"))
            {
                ID = jsonData.ID.ToString();

                if (jsonData.ContainsKey("EN"))
                {
                    EN = jsonData.EN.ToString();
                }
                if (jsonData.ContainsKey("DE"))
                {
                    DE = jsonData.DE.ToString();
                }
            }
            else
            {
                message = "Missing ID";
                return Json(new
                {
                    Success = success,
                    Message = message
                });
            }

            if (!string.IsNullOrEmpty(ID))
            {
                if (!string.IsNullOrEmpty(EN))
                {
                    XDocument enResource = await _CMSXMLFileService.LoadsXmlAsync("resources.en-us.xml");
                    XMLDocHelper enHelper = new XMLDocHelper(enResource);
                    enHelper.FlattenResourceFile();
                    enHelper.SetAppendRoot();
                    enHelper.ChangeValue(ID, EN);
                    enHelper.sortElements();
                    await _CMSXMLFileService.SaveXmlAsync(enHelper.Content, "resources.en-us.xml");
                    success = true;
                    message += "Successful update EN;";
                }

                if (!string.IsNullOrEmpty(DE))
                {
                    XDocument deResource = await _CMSXMLFileService.LoadsXmlAsync("resources.de-de.xml");
                    XMLDocHelper deHelper = new XMLDocHelper(deResource);
                    deHelper.ChangeValue(ID, DE);
                    deHelper.SetAppendRoot();
                    deHelper.sortElements();
                    await _CMSXMLFileService.SaveXmlAsync(deHelper.Content, "resources.de-de.xml");
                    success = true;
                    message += "Successful update DE;";
                }

                //await _CMSXMLFileService.UpdateStringResources();
                var languages = _languageService.GetAllLanguages();
                
                foreach (var item in languages)
                {
                    XDocument xDoc = await _CMSXMLFileService.LoadsXmlAsync($"resources.{item.LanguageCulture}.xml");
                    ImportModeFlags mode = ImportModeFlags.Update | ImportModeFlags.Insert;
                    await ImportResourcesFromXmlAsync(item, xDoc, null, true, mode, true);
                };
            }

            return Json(new
            {
                Success = success,
                Message = message
            });
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