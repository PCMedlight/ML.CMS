using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using System.Diagnostics;
using System.Text.Encodings.Web;
using Smartstore.Core.Localization;


namespace ML.CMS.Services
{
    public class L
    {
        private readonly Localizer _localizer;

        public L(Localizer localizer)
        {
            _localizer = localizer;
        }

        public HtmlString Get(string key)
        {
            string output = _localizer(key);
            bool IsAdmin = true;
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
