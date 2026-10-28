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
    public class ItinerariosController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Itinerarios
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Index()
        {
            var itinerario = db.Itinerario.Include(i => i.EstadoItinerario);
            return View(itinerario.ToList());
        }

        // GET: Itinerarios/Details/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Itinerario itinerario = db.Itinerario.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }
            return View(itinerario);
        }

        // GET: Itinerarios/Create
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create()
        {
            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerario, "Codigo", "Descripcion");
            return View();
        }

        // POST: Itinerarios/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create([Bind(Include = "ItinerarioId,Referencia,Nombre,EstadoItinerarioCodigo")] Itinerario itinerario)
        {
            if (ModelState.IsValid)
            {
                db.Itinerario.Add(itinerario);
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

            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerario, "Codigo", "Descripcion", itinerario.EstadoItinerarioCodigo);
            return View(itinerario);
        }

        // GET: Itinerarios/Edit/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Itinerario itinerario = db.Itinerario.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }
            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerario, "Codigo", "Descripcion", itinerario.EstadoItinerarioCodigo);
            return View(itinerario);
        }

        // POST: Itinerarios/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit([Bind(Include = "ItinerarioId,Referencia,Nombre,EstadoItinerarioCodigo")] Itinerario itinerario)
        {
            if (ModelState.IsValid)
            {
                db.Entry(itinerario).State = EntityState.Modified;
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
            ViewBag.EstadoItinerarioCodigo = new SelectList(db.EstadoItinerario, "Codigo", "Descripcion", itinerario.EstadoItinerarioCodigo);
            return View(itinerario);
        }

        // GET: Itinerarios/Delete/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Itinerario itinerario = db.Itinerario.Find(id);
            if (itinerario == null)
            {
                return HttpNotFound();
            }
            return View(itinerario);
        }

        // POST: Itinerarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult DeleteConfirmed(int id)
        {
            Itinerario itinerario = db.Itinerario.Find(id);
            db.Itinerario.Remove(itinerario);
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
