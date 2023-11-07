using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore;
using System.Xml;
using Smartstore.Core.DataExchange;
using System.Xml.Linq;
using System.IO;
using System.Threading;
using NUglify.Html;
//using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
//using static Smartstore.Core.Security.Permissions.Configuration;
using System.Globalization;
using Smartstore.Engine;

namespace ML.CMS.Helper
{
    public class CMSXMLFileService
    {

        private readonly SmartDbContext _db;
        private readonly ModuleManager _moduleManager;
        private readonly IModuleCatalog _moduleCatalog;
        private readonly IRequestCache _requestCache;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly IModuleDescriptor _moduleDescriptor;
        private readonly IXmlResourceManager _xmlResourceManager;
        private readonly IApplicationContext _appContext;

        public CMSXMLFileService(
            SmartDbContext db,
            ModuleManager moduleManager,
            IApplicationContext appContext,
            IModuleCatalog moduleCatalog,
            IRequestCache requestCache,
            ILanguageService languageService,
            ILocalizationService localizationService,
             IXmlResourceManager xmlResourceManager
            )
        {
            _db = db;
            _moduleManager = moduleManager;
            _appContext = appContext;
            _moduleCatalog = moduleCatalog;
            _requestCache = requestCache;
            _languageService = languageService;
            _localizationService = localizationService;
            _xmlResourceManager = xmlResourceManager;
            _moduleDescriptor = _moduleCatalog.GetModuleByName("ML.CMS");
        }


        public IEnumerable<string> ListBaks()
        {
            var directory = _moduleDescriptor.ContentRoot.GetDirectory("Localization");

            // List all .bak files in the directory.
            var directoryPath = Path.Combine(directory.PhysicalPath, "bak");
            var bakFiles = Directory.EnumerateFiles(directoryPath, "*.xml", SearchOption.TopDirectoryOnly);

            return bakFiles.Select(Path.GetFileNameWithoutExtension);
        }

        public async Task RestoreBak(string filename)
        {
            var directory = _moduleDescriptor.ContentRoot.GetDirectory("Localization");
            var filePath = Path.Combine(directory.PhysicalPath, filename);
            var bakFilePath = Path.Combine(directory.PhysicalPath, "bak", $"{filename}.xml");

            if (File.Exists(bakFilePath))
            {
                // Copy the backup file back to the original file.
                File.Copy(bakFilePath, filePath, true);
            }
            else
            {
                // Handle the case when the backup file does not exist.
                // You can throw an exception or handle it as needed.
            }
        }

        public async Task CreateBak(string filename)
        {
            var directory = _moduleDescriptor.ContentRoot.GetDirectory("Localization");
            var filePath = Path.Combine(directory.PhysicalPath, filename);

            if (File.Exists(filePath))
            {
                var bakFilePath = Path.Combine(directory.PhysicalPath, "bak", $"{filename}.xml");

                // Copy the original file to the backup file.
                File.Copy(filePath, bakFilePath, true);
            }
            else
            {
                // Handle the case when the file does not exist.
                // You can throw an exception or handle it as needed.
            }
        }

        public async Task<XDocument> LoadsXmlAsync( string filename, string theme = null)
        {
            string directory;
            string filePath;
            if (theme == null)
            {
                directory = _moduleDescriptor.PhysicalPath;
                filePath = Path.Combine(directory,"Localization", filename);
            }
            else
            {
                //var abc = _db.ApplicationContext.ThemesRoot.Root;
                directory = _appContext.ThemesRoot.Root;
                filePath = Path.Combine(directory,theme,"Localization", filename);
            }

            if (File.Exists(filePath))
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    return await XDocument.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None);
                }
            }
            else
            {
                return null;
            }
        }

        private XmlDocument ConvertToXMLDocument(XDocument xDoc)
        {
            string xmlString = xDoc.ToString();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);
            return xmlDoc;
        }

        public async Task UpdateStringResources()
        {
            var languages = _languageService.GetAllLanguages();
            //await _localizationService.DeleteLocaleStringResourcesAsync("ML.");
            foreach (var item in languages)
            {
                XDocument xDoc = await LoadsXmlAsync($"resources.{item.LanguageCulture}.xml");
                ImportModeFlags mode = ImportModeFlags.Update | ImportModeFlags.Insert;
                await ImportResourcesFromXmlAsync(item, xDoc, null, true, mode, true);
            };

            //await _xmlResourceManager.ImportModuleResourcesFromXmlAsync(_moduleDescriptor, null, true);
        }

        public async Task SaveXmlAsync(XDocument document, string filename, string theme = null)
        {
            string directory;
            string filePath;
            if (theme == null)
            {
                directory = _moduleDescriptor.PhysicalPath;
                filePath = Path.Combine(directory, "Localization", filename);
            }
            else
            {
                //var abc = _db.ApplicationContext.ThemesRoot.Root;
                directory = _appContext.ThemesRoot.Root;
                filePath = Path.Combine(directory, theme, "Localization", filename);
            }

            // Create a FileStream to write the XDocument to the file.
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                // Save the XDocument to the file asynchronously.
                await document.SaveAsync(fileStream, SaveOptions.None, CancellationToken.None);
            }
        }

        public async Task<int> ImportResourcesFromXmlAsync(
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
            //check if we have any children and return if yes
            if (xmlDocument.Root == null || (xmlDocument.Descendants("Children").Count() > 0) )
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
                return await _db.SaveChangesAsync();
            }

            return 0;
        }
        public virtual DirectoryHasher CreateModuleResourcesHasher(IModuleDescriptor moduleDescriptor)
        {
            try
            {
                return moduleDescriptor.ContentRoot.GetDirectoryHasher("Localization", "resources.*.xml");
            }
            catch
            {
                return null;
            }
        }

    }
}
