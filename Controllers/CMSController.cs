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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tga;
using DotLiquid.Tags;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
//using TinifyAPI;

namespace ML.CMS.Controllers
{

    [Area("Admin")]
    public class CMSController : ModuleController
    {
        private readonly SmartDbContext _db;
        private readonly ILanguageService _languageService;
        private readonly CMSXMLFileService _CMSXMLFileService;
        private readonly List<Language> _languages;

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


        public void ConvertToWebp(string inputFilePath, string outputFilePath, bool overwrite = false)
        {
            //webp encoder with 80% quality
            var encoder = new WebpEncoder
            {
                Quality = 80
            };
            using (var image = Image.Load(inputFilePath))
            {
                //convert to webp
                image.Save(outputFilePath, new WebpEncoder());
            }
        }


        [AuthorizeAdmin, Permission(CMSPermissions.Update)]
        [HttpPost]
        public async Task<IActionResult> DeleteFile(string fileUrl)
        {
            //reconstruct path if file fileUrl exists
            fileUrl = fileUrl?.Replace("/", "\\");
            if (fileUrl == null)
            {
                return BadRequest(new { message = "Invalid option." });
            }

            string themesRoot = this.Services.ApplicationContext.ThemesRoot.Root;
            string imagesRoot = Path.Combine(themesRoot, "MEDlight-Theme", "wwwroot", "images");
            string deleteableFile = Path.Combine(imagesRoot, fileUrl);
            if (System.IO.File.Exists(deleteableFile))
            {
                System.IO.File.Delete(deleteableFile);
                return Ok(new { message = "File deleted successfully." });
            }
            return BadRequest(new { message = "File not found." });
        }

        [AuthorizeAdmin, Permission(CMSPermissions.Update)]
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, string directory, bool overwrite = false)
        {
            if (directory == null || String.IsNullOrEmpty(directory))
            {
                return BadRequest(new { message = "Invalid option." });
            }

            var directoryRebuild = directory.Split("/");
            //remove empty entries
            directoryRebuild = Array.FindAll(directoryRebuild, s => !string.IsNullOrEmpty(s));
            //remove the first occurancce of "Themes" case insensitive
            List<string> directoryRebuildList = directoryRebuild.ToList();
            string[] directoriesToRemove = { "Themes", "MEDlight-Theme", "images" };

            foreach (string dirToRemove in directoriesToRemove)
            {
                directoryRebuildList.Remove(dirToRemove);
            }

            directoryRebuild = directoryRebuildList.ToArray();


            //reconstruct path using path.combine
            directory = Path.Combine(directoryRebuild);


            string themesRoot = this.Services.ApplicationContext.ThemesRoot.Root;
            string imagesRoot = Path.Combine(themesRoot, "MEDlight-Theme", "wwwroot", "images", directory);
            string outputFilePath = Path.Combine(imagesRoot, file.FileName);
            if (Path.GetExtension(file.FileName) == ".webp")
            {
                outputFilePath = Path.Combine(imagesRoot, Path.GetFileNameWithoutExtension(file.FileName) + ".png");
            }
            // Check if the file is not null and has content
            if (file != null && file.Length > 0)
            {

                using (var stream = file.OpenReadStream())
                {
                    string outputFilePathWebp = Path.Combine(imagesRoot, Path.GetFileNameWithoutExtension(file.FileName) + ".webp");
                    using (var img = Image.Load(stream))
                    {
                        var encoder = new WebpEncoder { Quality = 80 };
                        _ = img.SaveAsWebpAsync(outputFilePathWebp, encoder);
                    }
                }

                if (Path.GetExtension(file.FileName) == ".jpg")
                {
                    using (var stream = file.OpenReadStream())
                    {
                        using (var img = Image.Load(stream))
                        {
                            var encoder = new JpegEncoder { Quality = 80 };
                            _ = img.SaveAsync(outputFilePath, encoder);
                        }
                    }
                }

                if (Path.GetExtension(file.FileName) == ".png")
                {
                    using (var stream = file.OpenReadStream())
                    {
                        using (var img = Image.Load(stream))
                        {
                            var encoder = new PngEncoder { CompressionLevel = (PngCompressionLevel)9 };
                            _ = img.SaveAsync(outputFilePath, encoder);
                        }
                    }
                }

                //tinypng api key MY2P3RlkjB6VgbK8g1ybkjvCWTDqBlcY
                //Tinify.Key = "MY2P3RlkjB6VgbK8g1ybkjvCWTDqBlcY";
                //if (Path.GetExtension(file.FileName) == ".png")
                //{
                //    using (var stream = file.OpenReadStream())
                //    {
                //        var sourceData = new byte[stream.Length];
                //        await stream.ReadAsync(sourceData, 0, (int)stream.Length);
                //        var resultData = await Tinify.FromBuffer(sourceData).ToBuffer();
                //        using (var compressedStream = new MemoryStream(resultData))
                //        using (var outputFileStream = new FileStream(outputFilePath, FileMode.Create))
                //        {
                //            await compressedStream.CopyToAsync(outputFileStream);
                //        }
                //        stream.Close();
                //    }
                //}

                return Ok(new { message = "File uploaded successfully." });
            }

            return BadRequest(new { message = "No file or empty file." });
        }


        [AuthorizeAdmin, Permission(CMSPermissions.Read)]
        [HttpGet]
        public async Task<IActionResult> GetImages(CMSSettings settings)
        {
            string themesRoot = this.Services.ApplicationContext.ThemesRoot.Root;
            string imagesRoot = Path.Combine(themesRoot, "MEDlight-Theme", "wwwroot", "images");
            //Collect Images from dir  and all subdirectories
            var jpgsfiles = Directory.GetFiles(imagesRoot, "*.jpg", SearchOption.AllDirectories);
            var pngsfiles = Directory.GetFiles(imagesRoot, "*.png", SearchOption.AllDirectories);
            var webPfiles = Directory.GetFiles(imagesRoot, "*.webp", SearchOption.AllDirectories);
            // Create jsron response
            var imagefiles = Json(new
            {
                jpgs = jpgsfiles,
                pngs = pngsfiles,
                webp = webPfiles
            });


            // You can return the image paths or process them further as needed
            return Ok(imagefiles);
        }

        [AuthorizeAdmin, Permission(CMSPermissions.Update)]
        [HttpPost]
        public async Task<IActionResult> CreateBak()
        {
            return null;
        }

        [AuthorizeAdmin, Permission(CMSPermissions.Update)]
        [HttpGet]
        public async Task<IActionResult> ExportLanguageResource()
        {
            var success = false;
            dynamic message = string.Empty;


            List <XMLDocHelper> outputXML = new List<XMLDocHelper>();
            foreach (Language language in _languages)
            {
                XMLDocHelper xMLDoc = await ExtractResourceFromDB(language);
                outputXML.Add(xMLDoc);
            }


            message = outputXML;
            success = true;

            return Json(new
            {
                Success = success,
                Message = message
            });
        }

        private async Task<XMLDocHelper> ExtractResourceFromDB(Language selectedLanguage)
        {
            await _db.LoadCollectionAsync(selectedLanguage, x => x.LocaleStringResources);
            var resources = selectedLanguage.LocaleStringResources.ToDictionarySafe(x => x.ResourceName, StringComparer.OrdinalIgnoreCase);
            //Filter out all resource that dont start with "ML."
            var filteredResources = resources.Where(x => x.Key.StartsWith("ML.")).ToDictionary(x => x.Key, x => x.Value);
            XDocument xMLDoc = new XDocument();
            XElement root = new XElement("Language",
                            new XAttribute("Name", selectedLanguage.Name),
                            new XAttribute("IsDefault", "true"),
                            new XAttribute("IsRightToLeft", "false"));
            xMLDoc.Add(root);
            XMLDocHelper xMLDocHelper = new XMLDocHelper(xMLDoc);
            foreach (var item in filteredResources)
            {
                string ResourceValue = item.Value.ResourceValue;
                xMLDocHelper.ChangeValue(item.Key, ResourceValue);
            }
            xMLDocHelper.sortElements();
            return xMLDocHelper;
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
                return Json(new { Success = false, Message = "Invalid ID" });
            }

            string ID = jsonData["ID"].ToString();
            Dictionary<Language, string> updatedLanguageValues = _languages
                .Where(language => jsonData.ContainsKey(language.LanguageCulture))
                .ToDictionary(
                    language => language,
                    language => jsonData[language.LanguageCulture].ToString()
                );

            string languageCulture = string.Empty;
            try
            {
                foreach (var updatedValue in updatedLanguageValues)
                {
                    languageCulture = updatedValue.Key.LanguageCulture;
                    XDocument xResource = await _CMSXMLFileService.LoadsXmlAsync($"resources.{languageCulture}.xml");
                    XMLDocHelper xHelper = cleanXDoc(ID, updatedValue.Value, xResource);
                    await _CMSXMLFileService.SaveXmlAsync(xHelper.Content, $"resources.{languageCulture}.xml");
                    await ImportResourcesFromXmlAsync(updatedValue.Key, xHelper.Content);
                    message += $"Successful updated {languageCulture}; ";
                    success = true;
                }
            }
            catch (Exception ex)
            {
                success = false;
                message += $"Error updating {languageCulture}: {ex.Message}; ";
                Logger.Error("UpdateResource failed: {ex.Message}.", ex.Message, ex);
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
            var duplicates = xHelper.GetDuplicates();
            if (duplicates.Count > 0)
            {
                throw new Exception($"Duplicates found: {string.Join(",", duplicates)}");
            }
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