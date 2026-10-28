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
    public class ModeloAeronavesController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: ModeloAeronaves
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Index()
        {
            return View(db.ModeloAeronave.ToList());
        }

        // GET: ModeloAeronaves/Details/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ModeloAeronave modeloAeronave = db.ModeloAeronave.Find(id);
            if (modeloAeronave == null)
            {
                return HttpNotFound();
            }
            return View(modeloAeronave);
        }

        // GET: ModeloAeronaves/Create
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: ModeloAeronaves/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create([Bind(Include = "ModeloAeronaveId,Fabricante,Nombre,CapacidadAsientos")] ModeloAeronave modeloAeronave)
        {
            if (ModelState.IsValid)
            {
                db.ModeloAeronave.Add(modeloAeronave);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(modeloAeronave);
        }

        // GET: ModeloAeronaves/Edit/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ModeloAeronave modeloAeronave = db.ModeloAeronave.Find(id);
            if (modeloAeronave == null)
            {
                return HttpNotFound();
            }
            return View(modeloAeronave);
        }

        // POST: ModeloAeronaves/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit([Bind(Include = "ModeloAeronaveId,Fabricante,Nombre,CapacidadAsientos")] ModeloAeronave modeloAeronave)
        {
            if (ModelState.IsValid)
            {
                db.Entry(modeloAeronave).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(modeloAeronave);
        }

        // GET: ModeloAeronaves/Delete/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ModeloAeronave modeloAeronave = db.ModeloAeronave.Find(id);
            if (modeloAeronave == null)
            {
                return HttpNotFound();
            }
            return View(modeloAeronave);
        }

        // POST: ModeloAeronaves/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult DeleteConfirmed(int id)
        {
            ModeloAeronave modeloAeronave = db.ModeloAeronave.Find(id);
            db.ModeloAeronave.Remove(modeloAeronave);
            db.SaveChanges();
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
