using Microsoft.AspNetCore.Mvc.Filters;
using ML.CMS.Configuration;
using Smartstore;
using Smartstore.Core;
using Smartstore.Core.Widgets;
using Smartstore.Engine;

namespace ML.CMS.Filters
{
    public class MachineNameFilter : IResultFilter
    {
        private readonly ICommonServices _services;
        private readonly IWidgetProvider _widgetProvider;
        private readonly CMSSettings _cmsSettings;
        private readonly IApplicationContext _appContext;

        public MachineNameFilter(
            ICommonServices services,
            IWidgetProvider widgetProvider,
            CMSSettings cmsSettings,
            IApplicationContext appContext)
        {
            _services = services;
            _widgetProvider = widgetProvider;
            _cmsSettings = cmsSettings;
            _appContext = appContext;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!_cmsSettings.DisplayMachineName)
            {
                return;
            }

            if (!filterContext.HttpContext.Request.IsNonAjaxGet())
            {
                return;
            }

            // should only run on a full view rendering result or HTML ContentResult
            if (!filterContext.Result.IsHtmlViewResult())
            {
                return;
            }

            //_widgetProvider.RegisterViewComponent<MachineNameViewComponent>("end");
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
