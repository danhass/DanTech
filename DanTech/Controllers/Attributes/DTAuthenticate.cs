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
            if (!string.IsNullOrEmpty(session))
            {
                var sessionRecord = (from x in _db.dtSessions where x.session == session select x).FirstOrDefault();
                if (sessionRecord == null || sessionRecord.expires < DateTime.Now || sessionRecord.hostAddress != ipAddress)
                {
                    context.HttpContext.Response.Cookies.Delete("dtSessionId");
                    if (sessionRecord != null)
                    {
                        _db.dtSessions.Remove(sessionRecord);
                    }
                }
                else
                {
                    var user = (from x in _db.dtUsers where x.id == sessionRecord.user select x).FirstOrDefault();
                    if (user == null)
                    {
                        _db.dtSessions.Remove(sessionRecord);
                        context.HttpContext.Response.Cookies.Delete("dtSessionId");
                    }
                    else
                    {
                        var config = new MapperConfiguration(cfg =>
                        {
                            cfg.CreateMap<dtSession, dtSessionModel>();
                            cfg.CreateMap<dtUser, dtUserModel>().
                                ForMember(dest => dest.session, act => act.MapFrom(src => src.dtSession));
                        });
                        var mapper = new Mapper(config);
                        controller.VM.User = mapper.Map<dtUserModel>(user);
                        sessionRecord.expires = DateTime.Now.AddDays(1);
                    }
                }
                _db.SaveChanges();
            }
            base.OnActionExecuting(context);
        }
    }
}
