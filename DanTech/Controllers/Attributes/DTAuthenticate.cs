﻿using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using DanTech.Data.Models;
using DanTech.Services;
using System;
using System.Threading;

namespace DanTech.Controllers
{
    public class DTAuthenticate : ActionFilterAttribute
    {
        private readonly IConfiguration _configuration;
        private IDTDBDataService _db = null;
        private const string _testFlagKey = "Testing in progress";

        public DTAuthenticate(IConfiguration configuration, IDTDBDataService data)
        {
            _db = data;
            _configuration = configuration;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            //The session id must be the first parameter on the query string. If not then it can be a cookie on the request,
            //  but CORS can prevent receiving request cookies.
            var hostAddress = context.HttpContext.Connection.RemoteIpAddress.ToString();
            var path = context.HttpContext.Request.Path;

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
            dtUserModel userModel = null;
            userModel = _db.UserModelForSession(session, hostAddress);
            controller.VM.User = userModel;
            if (controller.VM.User == null) context.HttpContext.Response.Cookies.Delete("dtSessionId");


            //Since we are validating with the session, we are fine with CORS here.
            if (!context.HttpContext.Response.Headers.Keys.Contains("Access-Control-Allow-Origin"))
                context.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            base.OnActionExecuting(context);
        }
    }
}
