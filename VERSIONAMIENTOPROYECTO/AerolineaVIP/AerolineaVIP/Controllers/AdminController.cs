using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AerolineaVIP.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        // Pantalla 17: accesos a los módulos administrativos (sin dashboard)
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Index()
        {
            return View();
        }
    }
}