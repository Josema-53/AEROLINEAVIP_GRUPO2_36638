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
    public class PasajerosController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Pasajeros
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Index()
        {
            var query = db.Pasajero.Include("TipoDocumentoIdentidad").AsQueryable();

            // Filtro de búsqueda por documento, nombres o apellidos
            if (!string.IsNullOrEmpty(Request.QueryString["buscar"]))
            {
                string buscar = Request.QueryString["buscar"].Trim();
                query = query.Where(p => p.NumeroDocumento.Contains(buscar) || p.Nombres.Contains(buscar) || p.Apellidos.Contains(buscar));
            }
            if (!string.IsNullOrEmpty(Request.QueryString["tipo"]))
            {
                string tipo = Request.QueryString["tipo"];
                query = query.Where(p => p.TipoDocumentoCodigo == tipo);
            }
            ViewBag.tipo = new SelectList(db.TipoDocumentoIdentidad, "Codigo", "Descripcion", Request.QueryString["tipo"]);
            return View(query.OrderBy(p => p.Apellidos).ToList());
        }

        // GET: Pasajeros/Details/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pasajero pasajero = db.Pasajero.Find(id);
            if (pasajero == null)
            {
                return HttpNotFound();
            }
            return View(pasajero);
        }

        // GET: Pasajeros/Create
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create()
        {
            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidad, "Codigo", "Descripcion");
            return View();
        }

        // POST: Pasajeros/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create([Bind(Include = "PasajeroId,TipoDocumentoCodigo,NumeroDocumento,Nombres,Apellidos,FechaNacimiento,Correo,Telefono,FechaRegistro")] Pasajero pasajero)
        {
            if (ModelState.IsValid)
            {
                db.Pasajero.Add(pasajero);
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

            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidad, "Codigo", "Descripcion", pasajero.TipoDocumentoCodigo);
            return View(pasajero);
        }

        // GET: Pasajeros/Edit/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pasajero pasajero = db.Pasajero.Find(id);
            if (pasajero == null)
            {
                return HttpNotFound();
            }
            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidad, "Codigo", "Descripcion", pasajero.TipoDocumentoCodigo);
            return View(pasajero);
        }

        // POST: Pasajeros/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit([Bind(Include = "PasajeroId,TipoDocumentoCodigo,NumeroDocumento,Nombres,Apellidos,FechaNacimiento,Correo,Telefono,FechaRegistro")] Pasajero pasajero)
        {
            if (ModelState.IsValid)
            {
                db.Entry(pasajero).State = EntityState.Modified;
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
            ViewBag.TipoDocumentoCodigo = new SelectList(db.TipoDocumentoIdentidad, "Codigo", "Descripcion", pasajero.TipoDocumentoCodigo);
            return View(pasajero);
        }

        // GET: Pasajeros/Delete/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pasajero pasajero = db.Pasajero.Find(id);
            if (pasajero == null)
            {
                return HttpNotFound();
            }
            return View(pasajero);
        }

        // POST: Pasajeros/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult DeleteConfirmed(int id)
        {
            Pasajero pasajero = db.Pasajero.Find(id);
            db.Pasajero.Remove(pasajero);
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
