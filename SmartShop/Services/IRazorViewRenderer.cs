namespace SmartShop.Services
{
    public interface IRazorViewRenderer
    {
        Task<string> RenderAsync<TModel>(string viewPath, TModel model);
    }
}
