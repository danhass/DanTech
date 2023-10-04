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
        protected IDTDBDataService _db;
        protected dtUser _user = null;

        public DTViewModel VM { get; set; }

        protected void log(string entry, string key="Debug logging")
        {
            _db.Log(new dtMisc() { title = key, value = entry });
        }

        public DTController(IConfiguration configuration, ILogger<DTController> logger, IDTDBDataService data)
        {            
            _logger = logger;
            _configuration = configuration;

            _db = data;
            var rawDB = data.Instantiate(configuration);
            if (_db == null || rawDB == null)
            {
                throw new Exception("Database is null");
            }

            if (!DTConstants.Initialized()) DTConstants.Init(rawDB);
        }

        protected void SetVM(string sessionId)
        {
            VM = new DTViewModel();
            var session = _db.Sessions.Where (x => x.session == sessionId).FirstOrDefault();
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
