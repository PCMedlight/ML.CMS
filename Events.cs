using ML.CMS.Models;
using Smartstore.Events;
using Smartstore.Web.Rendering.Events;

namespace ML.CMS
{
    public class Events : IConsumer
    {
        public void HandleEvent(TabStripCreated message)
        {
            if (message.TabStripName == "product-edit")
            {
                //// Access the model
                //var productId = ((TabbableModel)message.Model).Id;

                //// Add in a widget into a dynamically created special tab called 'Plugins'
                //message.AddWidget(new ComponentWidgetInvoker(typeof(MyComponentWidget)));

                //// Add in a custom tab
                //await message.TabFactory.AddAsync(builder => builder
                //    .Text("My custom tab")
                //    .Name("tab-mycustom")
                //    .Icon("code", "bi")
                //    .LinkHtmlAttributes(new { data_tab_name = "DevTools" })
                //    .Action("ProductEditTab", "DevTools", new { productId })
                //    .Ajax());
            }
        }

        public void HandleEvent(ModelBoundEvent message)
        {
            if (!message.BoundModel.CustomProperties.ContainsKey("CMS"))
                return;

            var model = message.BoundModel.CustomProperties["CMS"] as BackendExtensionModel;
            if (model == null)
                return;

        }
    }
}