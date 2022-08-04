using DanTech.Data;
using DanTech.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DanTech.Models.Data;

namespace DanTech.Controllers
{
#nullable enable
    public class PlannerController : DTController
    {
        public PlannerController(IConfiguration configuration, ILogger<PlannerController> logger, dgdb dgdb) : base(configuration, logger, dgdb)
        {
        }
        [ServiceFilter(typeof(DTAuthenticate))]
        public IActionResult Index()
        {
            DTDBDataService svc = new DTDBDataService(_db);
            VM.PlanItems = svc.GetPlanItems(VM.User);
            return View(VM);
        }

        [HttpPost]
       
        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult SetPlanItem(string? title, string? note, string? start, string? startTime, string? end, string? endTime)
        {
            if (VM == null) return Json(null);
            DTDBDataService svc = new DTDBDataService(_db);
#pragma warning disable CS8604 // Possible null reference argument.
            var pi = new dtPlanItemModel(title, note, start, startTime, end, endTime, null, false, false, VM.User == null ? 0 : VM.User.id, VM.User , null, null, string.Empty, string.Empty);
#pragma warning restore CS8604 // Possible null reference argument.
            svc.Set(pi);
            var x = Json(svc.GetPlanItems(VM.User));
            return Json(svc.GetPlanItems(VM.User));
        }
    }
#nullable disable
}
