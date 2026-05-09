using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace SmartShop.Services
{
    public class RazorViewRenderer : IRazorViewRenderer
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public RazorViewRenderer(IRazorViewEngine viewEngine, ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RenderAsync<TModel>(string viewPath, TModel model)
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var viewResult = _viewEngine.GetView(null, viewPath, false);
            if (!viewResult.Success)
                throw new InvalidOperationException($"View not found: {viewPath}");

            using var writer = new StringWriter();
            var viewData = new ViewDataDictionary<TModel>(
                new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };
            var tempData = new TempDataDictionary(httpContext, _tempDataProvider);
            var viewContext = new ViewContext(
                actionContext, viewResult.View, viewData, tempData, writer, new HtmlHelperOptions());

            await viewResult.View.RenderAsync(viewContext);
            return writer.ToString();
        }
    }
}
