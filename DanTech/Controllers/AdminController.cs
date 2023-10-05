using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DanTech.Services;

namespace DanTech.Controllers
{
    public class AdminController : DTController
    {
        public AdminController(IConfiguration configuration, ILogger<AdminController> logger, IDTDBDataService data) :
        base(configuration, logger, data)
        {
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
            _db.SetUser(VM.User.id);
            _db.SetUserPW(pw);
            return View(VM);
        }
    }
}
