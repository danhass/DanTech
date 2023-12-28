using DanTech.Data;
using DanTech.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DanTech.Controllers
{
    public class NutritionController : DTController
    {
        public NutritionController(IConfiguration configuration, ILogger<AdminController> logger, IDTDBDataService data, dtdb dbctx) :  base(configuration, logger, data, dbctx)
        {
        }

        public JsonResult Index()
        {
            return Json("DanTech Nutrition Controller");
        }
    }
}
