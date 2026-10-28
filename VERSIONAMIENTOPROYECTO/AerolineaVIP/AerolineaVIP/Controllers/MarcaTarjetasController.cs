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
        public ActionResult Index()
        {
            return View(db.MarcaTarjeta.ToList());
        }

        // GET: MarcaTarjetas/Details/5
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
        public ActionResult Create()
        {
            return View();
        }

        // POST: MarcaTarjetas/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Codigo,Nombre,LongitudNumero,LongitudCodigoSeguridad,PrefijosReferencia,RutaLogo,Activa")] MarcaTarjeta marcaTarjeta)
        {
            if (ModelState.IsValid)
            {
                db.MarcaTarjeta.Add(marcaTarjeta);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(marcaTarjeta);
        }

        // GET: MarcaTarjetas/Edit/5
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
        public ActionResult Edit([Bind(Include = "Codigo,Nombre,LongitudNumero,LongitudCodigoSeguridad,PrefijosReferencia,RutaLogo,Activa")] MarcaTarjeta marcaTarjeta)
        {
            if (ModelState.IsValid)
            {
                db.Entry(marcaTarjeta).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(marcaTarjeta);
        }

        // GET: MarcaTarjetas/Delete/5
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
        public ActionResult DeleteConfirmed(string id)
        {
            MarcaTarjeta marcaTarjeta = db.MarcaTarjeta.Find(id);
            db.MarcaTarjeta.Remove(marcaTarjeta);
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
