using Smartstore.Core;
using Smartstore.Core.Localization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Web;
using Smartstore.Events;
using Smartstore.Web.Razor;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using System.ComponentModel;



namespace ML.CMS.Services
{
    public class CMSLocalizationService
    {
        private readonly IWorkContext _workContext;
        private Localizer? _localizer;
        private IHtmlHelper _htmlHelper;
        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly HomePageSettings _homePageSettings;
        private readonly IMessageFactory _messageFactory;
        private readonly PrivacySettings _privacySettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly CommonSettings _commonSettings;
        private readonly StoreInformationSettings _storeInformationSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public bool IsAdmin { get; private set; } = false;

        public CMSLocalizationService(
                IWorkContext workContext,
                Localizer localizer,
                IHtmlHelper htmlHelper,
                SmartDbContext db,
                IStoreContext storeContext,
                HomePageSettings homePageSettings,
                IMessageFactory messageFactory,
                PrivacySettings privacySettings,
                CaptchaSettings captchaSettings,
                CommonSettings commonSettings,
                StoreInformationSettings storeInformationSettings,
                IHttpContextAccessor httpContextAccessor)

        {
            _workContext = workContext;
            _localizer = localizer;
            _htmlHelper = htmlHelper;
            _db = db;
            _storeContext = storeContext;
            _homePageSettings = homePageSettings;
            _messageFactory = messageFactory;
            _privacySettings = privacySettings;
            _captchaSettings = captchaSettings;
            _commonSettings = commonSettings;
            _storeInformationSettings = storeInformationSettings;
            _httpContextAccessor = httpContextAccessor;

            IsAdmin = _httpContextAccessor.HttpContext.User.Claims.Any(x => x.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" && x.Value == "Administrators");

        }

        public HtmlString GetLocalizedValue(string key)
        {
            string output = _localizer(key);
            if (IsAdmin)
            {
                string startTag = $"<span data-cms=\"{key}\">";
                string endTag = "</span>";
                output = ($"{startTag}{output}{endTag}");
            }
            return new HtmlString(output);
        }
    }
}


