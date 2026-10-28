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
    public class MarcaTarjetasController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: MarcaTarjetas
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Index()
        {
            return View(db.MarcaTarjeta.ToList());
        }

        // GET: MarcaTarjetas/Details/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MarcaTarjeta marcaTarjeta = db.MarcaTarjeta.Find(id);
            if (marcaTarjeta == null)
            {
                return HttpNotFound();
            }
            return View(marcaTarjeta);
        }

        // GET: MarcaTarjetas/Create
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: MarcaTarjetas/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create([Bind(Include = "Codigo,Nombre,LongitudNumero,LongitudCodigoSeguridad,PrefijosReferencia,RutaLogo,Activa")] MarcaTarjeta marcaTarjeta)
        {
            if (ModelState.IsValid)
            {
                db.MarcaTarjeta.Add(marcaTarjeta);
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

            return View(marcaTarjeta);
        }

        // GET: MarcaTarjetas/Edit/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MarcaTarjeta marcaTarjeta = db.MarcaTarjeta.Find(id);
            if (marcaTarjeta == null)
            {
                return HttpNotFound();
            }
            return View(marcaTarjeta);
        }

        // POST: MarcaTarjetas/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit([Bind(Include = "Codigo,Nombre,LongitudNumero,LongitudCodigoSeguridad,PrefijosReferencia,RutaLogo,Activa")] MarcaTarjeta marcaTarjeta)
        {
            if (ModelState.IsValid)
            {
                db.Entry(marcaTarjeta).State = EntityState.Modified;
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
            return View(marcaTarjeta);
        }

        // GET: MarcaTarjetas/Delete/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MarcaTarjeta marcaTarjeta = db.MarcaTarjeta.Find(id);
            if (marcaTarjeta == null)
            {
                return HttpNotFound();
            }
            return View(marcaTarjeta);
        }

        // POST: MarcaTarjetas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult DeleteConfirmed(string id)
        {
            MarcaTarjeta marcaTarjeta = db.MarcaTarjeta.Find(id);
            db.MarcaTarjeta.Remove(marcaTarjeta);
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
