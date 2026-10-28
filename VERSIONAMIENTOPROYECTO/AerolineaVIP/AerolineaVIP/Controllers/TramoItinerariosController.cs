using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    public class TramoItinerariosController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: TramoItinerarios
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Index()
        {
            var tramoItinerario = db.TramoItinerario.Include(t => t.Itinerario).Include(t => t.Vuelo);
            return View(tramoItinerario.ToList());
        }

        // GET: TramoItinerarios/Details/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            return View(tramoItinerario);
        }

        // GET: TramoItinerarios/Create
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create()
        {
            ViewBag.ItinerarioId = new SelectList(db.Itinerario, "ItinerarioId", "Referencia");
            ViewBag.VueloId = new SelectList(TextoVuelos(), "VueloId", "Texto");
            return View();
        }

        // POST: TramoItinerarios/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create([Bind(Include = "TramoItinerarioId,ItinerarioId,VueloId,Orden")] TramoItinerario tramoItinerario)
        {
            if (ModelState.IsValid)
            {
                db.TramoItinerario.Add(tramoItinerario);
                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Los triggers y restricciones de la base devuelven el motivo del rechazo
                    ModelState.AddModelError("", "No se pudo guardar: " + ex.GetBaseException().Message);
                }
            }

            ViewBag.ItinerarioId = new SelectList(db.Itinerario, "ItinerarioId", "Referencia", tramoItinerario.ItinerarioId);
            ViewBag.VueloId = new SelectList(TextoVuelos(), "VueloId", "Texto", tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // GET: TramoItinerarios/Edit/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            ViewBag.ItinerarioId = new SelectList(db.Itinerario, "ItinerarioId", "Referencia", tramoItinerario.ItinerarioId);
            ViewBag.VueloId = new SelectList(TextoVuelos(), "VueloId", "Texto", tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // POST: TramoItinerarios/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit([Bind(Include = "TramoItinerarioId,ItinerarioId,VueloId,Orden")] TramoItinerario tramoItinerario)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tramoItinerario).State = EntityState.Modified;
                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Los triggers y restricciones de la base devuelven el motivo del rechazo
                    ModelState.AddModelError("", "No se pudo guardar: " + ex.GetBaseException().Message);
                }
            }
            ViewBag.ItinerarioId = new SelectList(db.Itinerario, "ItinerarioId", "Referencia", tramoItinerario.ItinerarioId);
            ViewBag.VueloId = new SelectList(TextoVuelos(), "VueloId", "Texto", tramoItinerario.VueloId);
            return View(tramoItinerario);
        }

        // GET: TramoItinerarios/Delete/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            if (tramoItinerario == null)
            {
                return HttpNotFound();
            }
            return View(tramoItinerario);
        }

        // POST: TramoItinerarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult DeleteConfirmed(int id)
        {
            TramoItinerario tramoItinerario = db.TramoItinerario.Find(id);
            db.TramoItinerario.Remove(tramoItinerario);
            try
            {
                db.SaveChanges();
            }
            catch (Exception)
            {
                TempData["Error"] = "No se puede eliminar: el registro tiene información relacionada.";
                return RedirectToAction("Delete", new { id = id });
            }
            return RedirectToAction("Index");
        }

        private IEnumerable<object> TextoVuelos()
        {
            return ReglasReserva.VuelosConDetalle(db).Where(v => v.FechaOperacion >= DbFunctions.AddDays(DateTime.Today, -7)).ToList()
                .OrderBy(v => ReglasReserva.Salida(v))
                .Select(v => new
                {
                    v.VueloId,
                    Texto = ReglasReserva.CodigoVuelo(v.VueloId) + " · " + v.FechaOperacion.ToString("dd/MM/yyyy") + " · "
                        + v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA + " → " + v.Frecuencia.Ruta.Aeropuerto.CodigoIATA + " " + v.Frecuencia.HoraSalida.ToString("hh\\:mm")
                });
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
