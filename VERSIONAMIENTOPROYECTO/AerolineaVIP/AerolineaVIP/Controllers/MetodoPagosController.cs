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
    public class MetodoPagosController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: MetodoPagos
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Index()
        {
            return View(db.MetodoPago.ToList());
        }

        // GET: MetodoPagos/Details/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MetodoPago metodoPago = db.MetodoPago.Find(id);
            if (metodoPago == null)
            {
                return HttpNotFound();
            }
            return View(metodoPago);
        }

        // GET: MetodoPagos/Create
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: MetodoPagos/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create([Bind(Include = "Codigo,Nombre,Activo")] MetodoPago metodoPago)
        {
            if (ModelState.IsValid)
            {
                db.MetodoPago.Add(metodoPago);
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

            return View(metodoPago);
        }

        // GET: MetodoPagos/Edit/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MetodoPago metodoPago = db.MetodoPago.Find(id);
            if (metodoPago == null)
            {
                return HttpNotFound();
            }
            return View(metodoPago);
        }

        // POST: MetodoPagos/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit([Bind(Include = "Codigo,Nombre,Activo")] MetodoPago metodoPago)
        {
            if (ModelState.IsValid)
            {
                db.Entry(metodoPago).State = EntityState.Modified;
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
            return View(metodoPago);
        }

        // GET: MetodoPagos/Delete/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MetodoPago metodoPago = db.MetodoPago.Find(id);
            if (metodoPago == null)
            {
                return HttpNotFound();
            }
            return View(metodoPago);
        }

        // POST: MetodoPagos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult DeleteConfirmed(string id)
        {
            MetodoPago metodoPago = db.MetodoPago.Find(id);
            db.MetodoPago.Remove(metodoPago);
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
