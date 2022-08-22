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
using System.Web.Http.Cors;

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

        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult Stati(string sessionId)
        {
            DTDBDataService svc = new DTDBDataService(_db);
            return Json(svc.GetStati());
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult ColorCodes(string sessionId)
        {
            DTDBDataService svc = new DTDBDataService(_db);
            return Json(svc.GetColorCodes());
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult Projects(string sessionId)
        {
            if (VM == null || VM.User == null) return Json(null);
            DTDBDataService svc = new DTDBDataService(_db);
            return Json(svc.DTProjects(VM.User.id));
        }


        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult PlanItems(string sessionId)
        {
            DTDBDataService svc = new DTDBDataService(_db);
            VM.PlanItems = svc.GetPlanItems(VM.User);
            return Json(VM.PlanItems);
        }

        [HttpPost]
        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult SetProject(string sessionId, string title, string shortCode, int status, int? colorCode=null, int? priority=null, int? sortOrder=null, string notes = "")
        {
            if (!Response.Headers.Keys.Contains("Access-Control-Allow-Origin")) 
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
            if (VM == null) return Json(null);
            var newProj = new dtProject()
            {
                colorCode = colorCode,
                notes = notes,
                priority = priority,
                shortCode = shortCode,
                sortOrder = sortOrder,
                status = status,
                title = title,
                user = VM.User.id
            };
            DTDBDataService svc = new DTDBDataService(_db);
            svc.Set(newProj);
            return Json(svc.DTProjects(VM.User.id));
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
