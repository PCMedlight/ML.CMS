#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Smartstore;
using Smartstore.Core;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets;
using Smartstore.Engine;
using Smartstore.Events;
using Smartstore.Web.Razor;
using System.Text.Encodings.Web;
using static Smartstore.Core.Security.Permissions.Customer;
using System.Text.RegularExpressions;

namespace ML.CMS.Models
{
    public abstract class MLMLSmartRazorPage : SmartRazorPage<dynamic>
    {
    }

    public abstract class MLSmartRazorPage<TModel> : RazorPage<TModel>
    {
        private IDisplayHelper? _display;
        private Localizer? _localizer;
        private IWorkContext? _workContext;
        private IEventPublisher? _eventPublisher;
        private IApplicationContext? _appContext;
        private IPageAssetBuilder? _assets;
        private IUserAgent? _userAgent;
        private ILinkResolver? _linkResolver;
        private ICommonServices? _services;

        public MLSmartRazorPage()
        {
        }

        protected HttpRequest Request
        {
            get => Context.Request;
        }

        protected IDisplayHelper Display
        {
            get => _display ??= Context.RequestServices.GetRequiredService<IDisplayHelper>();
        }

        protected Localizer T
        {
            get => _localizer ??= Context.RequestServices.GetRequiredService<Localizer>();
        }

        protected IWorkContext WorkContext
        {
            get => _workContext ??= Context.RequestServices.GetRequiredService<IWorkContext>();
        }

        protected IEventPublisher EventPublisher
        {
            get => _eventPublisher ??= Context.RequestServices.GetRequiredService<IEventPublisher>();
        }

        protected IApplicationContext ApplicationContext
        {
            get => _appContext ??= Context.RequestServices.GetRequiredService<IApplicationContext>();
        }

        protected IPageAssetBuilder Assets
        {
            get => _assets ??= Context.RequestServices.GetRequiredService<IPageAssetBuilder>();
        }

        protected IUserAgent UserAgent
        {
            get => _userAgent ??= Context.RequestServices.GetRequiredService<IUserAgent>();
        }

        protected ILinkResolver LinkResolver
        {
            get => _linkResolver ??= Context.RequestServices.GetRequiredService<ILinkResolver>();
        }

        protected ICommonServices CommonServices
        {
            get => _services ??= Context.RequestServices.GetRequiredService<ICommonServices>();
        }

        /// <summary>
        /// Resolves a service from scoped service container.
        /// </summary>
        /// <typeparam name="T">Type to resolve</typeparam>
        protected T? Resolve<T>() where T : notnull
        {
            return Context.RequestServices.GetService<T>();
        }


        private string ContainsHtmlTag(string input, string key)
        {
            // Regular expression to find HTML tags
            string pattern = "<[^>]*>";
            bool firstTagModified = false;
            // Replace the first HTML tag with a modified version
            string result = Regex.Replace(input, pattern, match =>
            {
                string tag = match.Value;
                if (!firstTagModified)
                {
                    firstTagModified = true;
                    string modifiedTag = tag.Insert(tag.Length - 1, $" data-cms=\"{key}\"");
                    return modifiedTag;
                }
                else
                {
                    return tag; // Leave other tags unchanged
                }
            });

            if (!firstTagModified)
            {
                string startTag = $"<span data-cms=\"{key}\">";
                string endTag = "</span>";
                return ($"{startTag}{input}{endTag}");
            }

            return result;
        }

        static List<string> PageLocalizerTracker = new List<string>();

        protected async Task<List<string>> GetLocalizerTrackerAsync()
        {
            return PageLocalizerTracker;
        }

        protected void InitLocalizerTracker()
        {
            PageLocalizerTracker = new List<string>();
        }

        protected string L(string key, bool track)
        {
            string output = T(key);
            bool IsAdmin = this.User.Claims.Any(c => c.Value == "Administrators");
            if (IsAdmin && track)
            {
                string startTag = $"<span data-cms=\"{key}\">";
                string endTag = "</span>";
                output = ($"{startTag}{output}{endTag}");
                //only add if unique
                if (!PageLocalizerTracker.Contains(output))
                {
                    PageLocalizerTracker.Add(output);
                }
            }
            return new string(key);
        }

        protected HtmlString L(string key)
        {
            string output = T(key);
            bool IsAdmin = this.User.Claims.Any(c => c.Value == "Administrators");
            if (IsAdmin)
            {
                output = ContainsHtmlTag(output, key);
            }
            return new HtmlString(output);
        }

        #region Metadata

        public bool HasMetadata(string name)
        {
            return TryGetMetadata<object>(name, out _);
        }

        /// <summary>
        /// Looks up an entry in ViewData dictionary first, then in ViewData.ModelMetadata.AdditionalValues dictionary
        /// </summary>
        /// <typeparam name="T">Actual type of value</typeparam>
        /// <param name="name">Name of entry</param>
        /// <returns>Result</returns>
        public T? GetMetadata<T>(string name)
        {
            TryGetMetadata<T>(name, out var value);
            return value;
        }

        /// <summary>
        /// Looks up an entry in ViewData dictionary first, then in ViewData.ModelMetadata.AdditionalValues dictionary
        /// </summary>
        /// <typeparam name="T">Actual type of value</typeparam>
        /// <param name="name">Name of entry</param>
        /// <param name="defaultValue">The default value to return if item does not exist.</param>
        /// <returns>Result</returns>
        public T? GetMetadata<T>(string name, T? defaultValue)
        {
            if (TryGetMetadata<T>(name, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Looks up an entry in ViewData dictionary first, then in ViewData.ModelMetadata.AdditionalValues dictionary
        /// </summary>
        /// <typeparam name="T">Actual type of value</typeparam>
        /// <param name="name">Name of entry</param>
        /// <returns><c>true</c> if the entry exists in any of the dictionaries, <c>false</c> otherwise</returns>
        public bool TryGetMetadata<T>(string name, [MaybeNullWhen(false)] out T? value)
        {
            value = default;

            var exists = ViewData.TryGetValue(name, out var raw);
            if (!exists)
            {
                exists = ViewData.ModelMetadata?.AdditionalValues?.TryGetValue(name, out raw) == true;
            }

            if (raw != null)
            {
                value = raw.Convert<T>();
            }

            return exists;
        }

        #endregion
    }
}
