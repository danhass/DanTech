using System.Web.Http;
using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using DanTech.Data;
using DanTech.Models;
using DanTech.Models.Data;
using DanTech.Controllers;
using Microsoft.AspNetCore.Http;
using AutoMapper;
using DanTech.Services;

namespace DanTech.Controllers
{     
    public class DTAuthenticate : ActionFilterAttribute
    {
        private readonly IConfiguration _configuration;
        private dgdb _db = null;

        public DTAuthenticate(IConfiguration configuration, dgdb db)
        {
            _db = db;
            _configuration = configuration;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress.ToString();
            var session = context.HttpContext.Request.Cookies["dtSessionId"];
            var controller = (DTController)context.Controller;
            var host = context.HttpContext.Request.Host;
            controller.VM = new DTViewModel();
            controller.VM.TestEnvironment = host.ToString().StartsWith("localhost");
            var dataService = new DTDBDataService(_db);
            controller.VM.IsTesting = dataService.InTesting;
            controller.VM.User = dataService.UserModelForSession(session, ipAddress);
            if (controller.VM.User == null) context.HttpContext.Response.Cookies.Delete("dtSessionId");
            base.OnActionExecuting(context);
        }
    }
}
