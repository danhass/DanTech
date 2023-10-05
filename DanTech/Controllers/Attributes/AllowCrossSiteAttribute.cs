using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DanTech.Controllers
{
    public class AllowCrossSiteAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpResponse response = filterContext.HttpContext.Response;

            response.Headers.Add("Access-Control-Allow-Credentials", "true");

            base.OnActionExecuting(filterContext);
        }
    }
}