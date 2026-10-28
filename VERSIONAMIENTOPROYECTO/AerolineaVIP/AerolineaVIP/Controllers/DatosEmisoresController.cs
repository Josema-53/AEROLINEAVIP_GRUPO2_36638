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
    public class DatosEmisoresController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: DatosEmisores
        public ActionResult Index()
        {
            return View(db.DatosEmisor.ToList());
        }

        // GET: DatosEmisores/Details/5
        public ActionResult Details(byte? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DatosEmisor datosEmisor = db.DatosEmisor.Find(id);
            if (datosEmisor == null)
            {
                return HttpNotFound();
            }
            return View(datosEmisor);
        }

        // GET: DatosEmisores/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DatosEmisores/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "DatosEmisorId,NombreComercial,Identificacion,Direccion,Telefono,Correo,LeyendaComprobante")] DatosEmisor datosEmisor)
        {
            if (ModelState.IsValid)
            {
                db.DatosEmisor.Add(datosEmisor);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(datosEmisor);
        }

        // GET: DatosEmisores/Edit/5
        public ActionResult Edit(byte? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DatosEmisor datosEmisor = db.DatosEmisor.Find(id);
            if (datosEmisor == null)
            {
                return HttpNotFound();
            }
            return View(datosEmisor);
        }

        // POST: DatosEmisores/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DatosEmisorId,NombreComercial,Identificacion,Direccion,Telefono,Correo,LeyendaComprobante")] DatosEmisor datosEmisor)
        {
            if (ModelState.IsValid)
            {
                db.Entry(datosEmisor).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(datosEmisor);
        }

        // GET: DatosEmisores/Delete/5
        public ActionResult Delete(byte? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DatosEmisor datosEmisor = db.DatosEmisor.Find(id);
            if (datosEmisor == null)
            {
                return HttpNotFound();
            }
            return View(datosEmisor);
        }

        // POST: DatosEmisores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(byte id)
        {
            DatosEmisor datosEmisor = db.DatosEmisor.Find(id);
            db.DatosEmisor.Remove(datosEmisor);
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
