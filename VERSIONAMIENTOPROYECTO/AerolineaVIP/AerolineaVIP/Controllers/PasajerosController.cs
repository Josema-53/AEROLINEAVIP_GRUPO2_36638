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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create([Bind(Include = "TipoDocumentoCodigo,NumeroDocumento,Nombres,Apellidos,FechaNacimiento,Correo,Telefono")] Pasajero pasajero)
        {
            ValidarPasajero(pasajero);
            if (ModelState.IsValid)
            {
                pasajero.FechaRegistro = DateTime.Now;
                db.Pasajero.Add(pasajero);
                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "No se pudo guardar. Verifique los datos e inténtelo nuevamente.");
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit([Bind(Include = "PasajeroId,TipoDocumentoCodigo,NumeroDocumento,Nombres,Apellidos,FechaNacimiento,Correo,Telefono")] Pasajero pasajero)
        {
            ValidarPasajero(pasajero);
            var actual = db.Pasajero.Find(pasajero.PasajeroId);
            if (actual == null)
            {
                return HttpNotFound();
            }
            if (ModelState.IsValid)
            {
                actual.TipoDocumentoCodigo = pasajero.TipoDocumentoCodigo;
                actual.NumeroDocumento = pasajero.NumeroDocumento;
                actual.Nombres = pasajero.Nombres;
                actual.Apellidos = pasajero.Apellidos;
                actual.FechaNacimiento = pasajero.FechaNacimiento;
                actual.Correo = pasajero.Correo;
                actual.Telefono = pasajero.Telefono;
                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "No se pudo guardar. Verifique los datos e inténtelo nuevamente.");
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
            if (pasajero == null)
            {
                return HttpNotFound();
            }
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

        private void ValidarPasajero(Pasajero pasajero)
        {
            pasajero.NumeroDocumento = (pasajero.NumeroDocumento ?? "").Trim().ToUpperInvariant();
            pasajero.Nombres = (pasajero.Nombres ?? "").Trim();
            pasajero.Apellidos = (pasajero.Apellidos ?? "").Trim();
            pasajero.Correo = string.IsNullOrWhiteSpace(pasajero.Correo) ? null : pasajero.Correo.Trim();
            pasajero.Telefono = string.IsNullOrWhiteSpace(pasajero.Telefono) ? null : pasajero.Telefono.Trim();

            if (!db.TipoDocumentoIdentidad.Any(t => t.Codigo == pasajero.TipoDocumentoCodigo))
            {
                ModelState.AddModelError("TipoDocumentoCodigo", "Seleccione un tipo de documento válido.");
            }
            if (string.IsNullOrWhiteSpace(pasajero.NumeroDocumento) || pasajero.NumeroDocumento.Length > 20)
            {
                ModelState.AddModelError("NumeroDocumento", "Ingrese un documento válido de hasta 20 caracteres.");
            }
            else if (pasajero.TipoDocumentoCodigo == "CEDULA" && !ReglasReserva.ValidarCedula(pasajero.NumeroDocumento))
            {
                ModelState.AddModelError("NumeroDocumento", "La cédula ingresada no es válida.");
            }
            if (string.IsNullOrWhiteSpace(pasajero.Nombres) || string.IsNullOrWhiteSpace(pasajero.Apellidos))
            {
                ModelState.AddModelError("", "Ingrese los nombres y apellidos del pasajero.");
            }
            if (pasajero.FechaNacimiento.HasValue && (pasajero.FechaNacimiento.Value.Date > DateTime.Today || pasajero.FechaNacimiento.Value.Year <= 1900))
            {
                ModelState.AddModelError("FechaNacimiento", "La fecha de nacimiento no es válida.");
            }
            if (db.Pasajero.Any(p => p.TipoDocumentoCodigo == pasajero.TipoDocumentoCodigo
                && p.NumeroDocumento == pasajero.NumeroDocumento && p.PasajeroId != pasajero.PasajeroId))
            {
                ModelState.AddModelError("NumeroDocumento", "Ya existe un pasajero con ese documento.");
            }
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