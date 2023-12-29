using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DanTech.Services;
using DanTech.Data;
using System.Linq;
using DTUserManagement.Services;

namespace DanTech.Controllers
{
    public class AdminController : DTController
    {
        public AdminController(IConfiguration configuration, ILogger<AdminController> logger, IDTDBDataService data, dtdb dbctx) : base(configuration, logger, data, dbctx)
        {
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public IActionResult Index()
        {
            var v = VM;
            return View(VM);
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult SetPW(string sessionId, string pw)
        {
            if (VM == null || VM.User == null) return Json(null);
            _db.SetUser(VM.User.id);
            _db.SetUserPW(pw);
            return Json("Password set");
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult SetOrClearDoNotSetPWFlag(string sessionId, bool flag)
        {
            if (VM == null || VM.User == null) { return Json(null); }
            var u = _db.Users.Where(x => x.id == VM.User.id).FirstOrDefault();
            if (u == null) { return Json(null); }
            if (flag) { u.doNotSetPW = true; u.pw = null; }
            else { u.doNotSetPW = null; }
            _db.Set(u);
            return Json("Do Not Set Flag adjustment successfully completed.");
        }

        public JsonResult CompleteRegistration(string email, string regKey)
        {
            string addr = HttpContext.Connection.RemoteIpAddress.ToString();
            var svc = new DTRegistration(_db);
            var session = svc.CompleteRegistration(email, regKey, addr);
            if (!string.IsNullOrEmpty(session))
            {
                HttpContext.Response.Cookies.Append("dtSession", session);
            }
            return Json(new { session });
        }
    }
}
