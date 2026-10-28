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
    public class RutasController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Rutas
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Index()
        {
            var ruta = db.Ruta.Include(r => r.Aeropuerto).Include(r => r.Aeropuerto1);
            return View(ruta.ToList());
        }

        // GET: Rutas/Details/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Ruta.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            return View(ruta);
        }

        // GET: Rutas/Create
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create()
        {
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuerto, "AeropuertoId", "Localidad");
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuerto, "AeropuertoId", "Localidad");
            return View();
        }

        // POST: Rutas/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create([Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,TarifaReferencial,Activa")] Ruta ruta)
        {
            if (ModelState.IsValid)
            {
                db.Ruta.Add(ruta);
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

            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuerto, "AeropuertoId", "Localidad", ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuerto, "AeropuertoId", "Localidad", ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // GET: Rutas/Edit/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Ruta.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuerto, "AeropuertoId", "Localidad", ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuerto, "AeropuertoId", "Localidad", ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // POST: Rutas/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit([Bind(Include = "RutaId,AeropuertoOrigenId,AeropuertoDestinoId,TarifaReferencial,Activa")] Ruta ruta)
        {
            if (ModelState.IsValid)
            {
                db.Entry(ruta).State = EntityState.Modified;
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
            ViewBag.AeropuertoDestinoId = new SelectList(db.Aeropuerto, "AeropuertoId", "Localidad", ruta.AeropuertoDestinoId);
            ViewBag.AeropuertoOrigenId = new SelectList(db.Aeropuerto, "AeropuertoId", "Localidad", ruta.AeropuertoOrigenId);
            return View(ruta);
        }

        // GET: Rutas/Delete/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ruta ruta = db.Ruta.Find(id);
            if (ruta == null)
            {
                return HttpNotFound();
            }
            return View(ruta);
        }

        // POST: Rutas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult DeleteConfirmed(int id)
        {
            Ruta ruta = db.Ruta.Find(id);
            db.Ruta.Remove(ruta);
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
