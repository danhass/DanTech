﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DanTech.Data;
using DanTech.Services;

namespace DanTech.Controllers
{
    public class AdminController : DTController
    {
        private DTDBDataService _svc;

        public AdminController(IConfiguration configuration, ILogger<AdminController> logger, dtdb dtdb) :
        base(configuration, logger, dtdb)
        {
            _svc = new DTDBDataService(dtdb, _configuration.GetConnectionString("dg"));
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public IActionResult Index()
        {
            var v = VM;
            return View(VM);
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public IActionResult SetPW(string sessionId, string pw)
        {
            if (VM == null || VM.User == null) return Json(null);
            _svc.SetUser(VM.User.id);

            return View(VM);
        }

    }
}
