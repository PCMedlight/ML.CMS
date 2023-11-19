using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ML.CMS.Helper;
using ML.CMS.Services;
using Smartstore;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.OutputCache;
using Smartstore.Core.Web;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.Diagnostics;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Web.Controllers;

namespace ML.CMS
{
    internal class Startup : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
            => appContext.IsInstalled;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddTransient<IDbContextConfigurationSource<SmartDbContext>, SmartDbContextConfigurer>();
            if (appContext.IsInstalled)
            {
                services.AddScoped<CMSLocalizationService>();
                services.AddScoped<CMSProductService>();
                services.AddScoped<CMSXMLFileService>();
                services.AddScoped<L>();
                //services.AddScoped<ICMSLocalizationService>();
            }

        }

 
        internal static bool ResultsAuthorize(HttpRequest request)
        {
            var ua = request.HttpContext.RequestServices.GetRequiredService<IUserAgent>();
            if (ua.IsPdfConverter() || ua.IsBot())
            {
                return false;
            }

            return request.HttpContext.RequestServices.GetRequiredService<IWorkContext>().CurrentCustomer.IsAdmin();
        }

        class SmartDbContextConfigurer : IDbContextConfigurationSource<SmartDbContext>
        {
            public void Configure(IServiceProvider services, DbContextOptionsBuilder builder)
            {
                builder.UseDbFactory(b =>
                {
                    b.AddModelAssembly(GetType().Assembly);
                });
            }
        }
    }
}