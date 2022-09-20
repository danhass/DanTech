﻿using DanTech.Data;
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
using System.Threading;

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
            DTDBDataService svc = new DTDBDataService(_db, _configuration.GetConnectionString("dg"));
            VM.PlanItems = svc.PlanItems(VM.User.id);
            return View(VM);
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult Recurrences(string sessionId)
        {
            DTDBDataService svc = new DTDBDataService(_db, _configuration.GetConnectionString("dg"));
            var result = Json(svc.Recurrences());
            DTDBDataService.ClearBusy();
            return result;
         }

        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult Stati(string sessionId)
        {
            DTDBDataService svc = new DTDBDataService(_db, _configuration.GetConnectionString("dg"));
            var result = Json(svc.Stati());
            DTDBDataService.ClearBusy();
            return result;
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult ColorCodes(string sessionId)
        {
            DTDBDataService svc = new DTDBDataService(_db, _configuration.GetConnectionString("dg"));
            var result = Json(svc.ColorCodes());
            DTDBDataService.ClearBusy();
            return result;
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult Projects(string sessionId)
        {
            if (VM == null || VM.User == null) return Json(null);
            DTDBDataService svc = new DTDBDataService(_db, _configuration.GetConnectionString("dg"));
            var projs = svc.DTProjects(VM.User.id);
            DTDBDataService.ClearBusy();
            return Json(projs);
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult DeletePlanItem(string sessionId, int planItemId)
        {
            DTDBDataService svc = new DTDBDataService(_db, _configuration.GetConnectionString("dg"));
            if (VM == null || VM.User == null) return Json(null);
            var result = svc.DeletePlanItem(planItemId, VM.User.id);
            DTDBDataService.ClearBusy();
            var json = Json(result);

            return json;
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult PlanItems(string sessionId, int? daysBack = 1, bool? includeCompleted = false, bool? getAll = false, int? onlyProject = 0, bool? onlyRecurrences = false)
        {
            if (VM == null || VM.User == null) return Json(null);
            DTDBDataService svc = new DTDBDataService(_db, _configuration.GetConnectionString("dg"));
            VM.PlanItems = svc.PlanItems(VM.User.id
                , daysBack ?? 1
                , includeCompleted ?? false
                , getAll ?? false
                , onlyProject ?? 0
                , onlyRecurrences ?? false);
            DTDBDataService.ClearBusy();
            svc.SetConnString(_configuration.GetConnectionString("dg"));
            svc.SetUser(VM.User.id);
            //svc.LaunchUpdateRecurrences();
            return Json(VM.PlanItems);
        }

        [HttpPost]
        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult SetProject(string sessionId, string title, string shortCode, int status, int? colorCode=null, int? priority=null, int? sortOrder=null, string notes = "", int? id= null)
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
                user = VM.User.id,
                id = id.HasValue ? id.Value : 0
            };
            DTDBDataService svc = new DTDBDataService(_db, _configuration.GetConnectionString("dg"));
            svc.Set(newProj);
            var projs = svc.DTProjects(VM.User.id);
            DTDBDataService.ClearBusy();
            return Json(projs);
        }

        [HttpPost]
        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult SetPlanItem(string sessionId,
                                      string title,
                                      string? note,
                                      string? start,
                                      string? startTime,
                                      string? end,
                                      string? endTime,
                                      int? priority,
                                      bool? addToCalendar,
                                      bool? completed,
                                      bool? preserve,
                                      int? projectId,
                                      int? daysBack = 1,
                                      bool? includeCompleted = true,
                                      bool? getAll = false,
                                      int? onlyProject = 0,
                                      int? id = null,
                                      int? recurrence = null,
                                      string? recurrenceData = null
                                      )
        {
            if (VM == null) return Json(null);
            DTDBDataService svc = new DTDBDataService(_db, _configuration.GetConnectionString("dg"));
            svc.SetConnString(_configuration.GetConnectionString("dg"));
            var pi = new dtPlanItemModel(title,
                                         note,
                                         start,
                                         startTime,
                                         end,
                                         endTime,
                                         priority,
                                         addToCalendar,
                                         completed,
                                         preserve,
                                         VM.User == null ? 0 : VM.User.id,
                                         VM.User ?? new dtUserModel(),
                                         projectId,
                                         new dtProject(),
                                         false,
                                         id,
                                         recurrence,
                                         recurrenceData
                                         );
            svc.Set(pi);
            int uid = 0;
            if (VM != null) if (VM.User != null) uid = VM.User.id;
            var items = svc.PlanItems(VM.User.id,
                                        daysBack ?? 1, 
                                        includeCompleted ?? false, 
                                        getAll ?? false,
                                        onlyProject ?? 0);
            DTDBDataService.ClearBusy();
            return Json(items);
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult Propagate(string sessionId, int seedId)
        {
            if (VM == null) return Json(null);
            DTDBDataService svc = new DTDBDataService(_db, _configuration.GetConnectionString("dg"));
            var result = Json(svc.Propagate(seedId, VM.User.id));
            DTDBDataService.ClearBusy();
            return result;
        }
    }
#nullable disable
}
