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
using DanTech.Services;

namespace DanTech.Controllers
{
    public class DTController : Controller
    {
        protected readonly IConfiguration _configuration;
        protected readonly ILogger<DTController> _logger;
        protected static dtdb _db = null;
        protected DTDBDataService _svc;
        protected dtUser _user = null;

        public DTViewModel VM { get; set; }

        protected void log(string entry, string key="Debug logging")
        {
            _db.dtMiscs.Add(new dtMisc() { title = key, value = entry });
            _db.SaveChanges();
        }

        public DTController(IConfiguration configuration, ILogger<DTController> logger, dtdb dtdb)
        {
            _db = dtdb;
            _logger = logger;
            _configuration = configuration;
            if (_db == null)
            {
                throw new Exception("Database is null");
            }

            _svc = new DTDBDataService(_db, _configuration.GetConnectionString("dg"));

            if (!DTConstants.Initialized()) DTConstants.Init(_db);
        }

        protected void SetVM(string sessionId)
        {
            VM = new DTViewModel();
            var session = (from x in _db.dtSessions where x.session == sessionId select x).FirstOrDefault();
            if (session != null && session.userNavigation != null)
            {
                var user = session.userNavigation;
                if (user != null)
                {
                    var config = dtUserModel.mapperConfiguration;
                    var mapper = new Mapper(config);
                    VM.User = mapper.Map<dtUserModel>(user);
                }
            }
        }
}
}
