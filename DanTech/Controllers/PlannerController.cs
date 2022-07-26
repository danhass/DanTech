using DanTech.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DanTech.Controllers
{
    public class PlannerController : DTController
    {
        public PlannerController(IConfiguration configuration, ILogger<PlannerController> logger, dgdb dgdb) : base(configuration, logger, dgdb)
        {

        }
        [ServiceFilter(typeof(DTAuthenticate))]
        public IActionResult Index()
        {
            return View(VM);
        }
    }
}
