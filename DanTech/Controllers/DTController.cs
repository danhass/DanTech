using DanTech.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DanTech.Models;
using DanTech.Models.Data;
using AutoMapper;

namespace DanTech.Controllers
{
    public class DTController : Controller
    {
        protected readonly IConfiguration _configuration;
        protected readonly ILogger<DTController> _logger;
        protected dgdb _db = new dgdb();
        protected dtUser _user = null;

        public DTViewModel VM { get; set; }

        public DTController(IConfiguration configuration, ILogger<DTController> logger, dgdb dgdb)
        {
            _db = dgdb;
            _logger = logger;
            _configuration = configuration;        
        }

        protected void SetVM(string sessionId)
        {
            VM = new DTViewModel();
            var session = (from x in _db.dtSessions where x.session == sessionId select x).FirstOrDefault();
            if (session != null)
            {
                var user = (from x in _db.dtUsers where x.id == session.user select x).FirstOrDefault();
                if (user != null)
                {
                    var config = new MapperConfiguration(cfg =>
                    {
                        cfg.CreateMap<dtSession, dtSessionModel>();
                        cfg.CreateMap<dtUser, dtUserModel>().
                            ForMember(dest => dest.session, act => act.MapFrom(src => src.dtSession));
                    });
                    var mapper = new Mapper(config);
                    VM.User = mapper.Map<dtUserModel>(user);
                }
            }
        }
}
}
