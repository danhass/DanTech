﻿using System.Web.Http;
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
using System.IO;

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

            //The session id must be the first parameter on the query string. If not then it can be a cookie on the request,
            //  but CORS can prevent receiving request cookies.
            var hostAddress = context.HttpContext.Request.Host.Value;
            var session = context.HttpContext.Request.QueryString.Value.StartsWith("?sessionId=") ? 
                   context.HttpContext.Request.QueryString.Value.Split("&")[0].Split("=")[1] :
                   context.HttpContext.Request.Cookies["dtSessionId"];
            if (string.IsNullOrEmpty(session) && context.ActionArguments != null && context.ActionArguments.ContainsKey("sessionId"))
            {
                var sessionObj = context.ActionArguments["sessionId"];
                if (sessionObj is string) session = sessionObj.ToString();
            }
            var controller = (DTController)context.Controller;
            var host = context.HttpContext.Request.Host;
            controller.VM = new DTViewModel();
            controller.VM.TestEnvironment = host.ToString().StartsWith("localhost");
            var dataService = new DTDBDataService(_db);
            controller.VM.IsTesting = dataService.InTesting;
            controller.VM.User = dataService.UserModelForSession(session, hostAddress);            
            if (controller.VM.User == null) context.HttpContext.Response.Cookies.Delete("dtSessionId");

            //Since we are validating with the session, we are fine with CORS here.
            if (!context.HttpContext.Response.Headers.Keys.Contains("Access-Control-Allow-Origin")) 
                context.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            base.OnActionExecuting(context);
        }
    }
}
