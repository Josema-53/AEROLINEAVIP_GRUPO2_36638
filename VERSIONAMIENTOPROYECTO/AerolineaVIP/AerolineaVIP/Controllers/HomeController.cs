using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    public class HomeController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Home (Pantalla 1: inicio público con búsqueda rápida)
        public ActionResult Index()
        {
            var origenes = db.Ruta.Where(r => r.Activa).Select(r => r.AeropuertoOrigenId).Distinct().ToList();
            ViewBag.OrigenId = new SelectList(
                db.Aeropuerto.Where(a => a.Activo && origenes.Contains(a.AeropuertoId))
                             .OrderBy(a => a.Localidad).ToList()
                             .Select(a => new { a.AeropuertoId, Texto = a.Localidad + " (" + a.CodigoIATA + ")" }),
                "AeropuertoId", "Texto");
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Sistema de reservas de Aerolínea VIP.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Contacto.";

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
