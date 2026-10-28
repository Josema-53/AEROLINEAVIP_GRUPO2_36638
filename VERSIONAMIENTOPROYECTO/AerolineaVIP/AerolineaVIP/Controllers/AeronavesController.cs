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
    public class AeronavesController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Aeronaves
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Index()
        {
            var aeronave = db.Aeronave.Include(a => a.EstadoAeronave).Include(a => a.ModeloAeronave);

            // Vuelos próximos asignados a cada aeronave (no cancelados)
            var hoy = DateTime.Today;
            ViewBag.Proximos = db.Vuelo
                .Where(v => v.AeronaveId != null && v.FechaOperacion >= hoy && v.EstadoVueloCodigo != "CANCELADO")
                .GroupBy(v => v.AeronaveId.Value)
                .Select(g => new { Id = g.Key, Total = g.Count() })
                .ToList()
                .ToDictionary(x => x.Id, x => x.Total);
            return View(aeronave.ToList());
        }

        // GET: Aeronaves/Details/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronave.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }

            // Distribución de asientos del modelo (solo lectura) y próximas asignaciones
            ViewBag.Asientos = db.Asiento.Where(s => s.ModeloAeronaveId == aeronave.ModeloAeronaveId).OrderBy(s => s.NumeroAsiento).ToList()
                .Select(s => new AsientoViewModel { NumeroAsiento = s.NumeroAsiento, Codigo = s.Codigo, Fila = s.Fila, Lado = s.Lado, Ubicacion = s.Ubicacion, Estado = "DISPONIBLE" })
                .ToList();
            var hoy = DateTime.Today;
            int aeronaveId = aeronave.AeronaveId;
            ViewBag.Vuelos = ReglasReserva.VuelosConDetalle(db)
                .Where(v => v.AeronaveId == aeronaveId && v.FechaOperacion >= hoy)
                .ToList()
                .OrderBy(v => ReglasReserva.Salida(v))
                .ToList();
            return View(aeronave);
        }

        // GET: Aeronaves/Create
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create()
        {
            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronave, "Codigo", "Descripcion");
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Nombre");
            return View();
        }

        // POST: Aeronaves/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create([Bind(Include = "AeronaveId,Matricula,ModeloAeronaveId,EstadoAeronaveCodigo")] Aeronave aeronave)
        {
            if (ModelState.IsValid)
            {
                db.Aeronave.Add(aeronave);
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

            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronave, "Codigo", "Descripcion", aeronave.EstadoAeronaveCodigo);
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Nombre", aeronave.ModeloAeronaveId);
            return View(aeronave);
        }

        // GET: Aeronaves/Edit/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronave.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronave, "Codigo", "Descripcion", aeronave.EstadoAeronaveCodigo);
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Nombre", aeronave.ModeloAeronaveId);
            return View(aeronave);
        }

        // POST: Aeronaves/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit([Bind(Include = "AeronaveId,Matricula,ModeloAeronaveId,EstadoAeronaveCodigo")] Aeronave aeronave)
        {
            if (ModelState.IsValid)
            {
                db.Entry(aeronave).State = EntityState.Modified;
                try
                {
                    db.SaveChanges();
                    var hoy = DateTime.Today;
                    int proximos = db.Vuelo.Count(v => v.AeronaveId == aeronave.AeronaveId && v.FechaOperacion >= hoy && v.EstadoVueloCodigo != "CANCELADO");
                    if (aeronave.EstadoAeronaveCodigo == "NO_DISPONIBLE" && proximos > 0)
                    {
                        TempData["Mensaje"] = "La aeronave " + aeronave.Matricula + " tiene " + proximos + " vuelos asignados próximamente. Revise la programación.";
                    }
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // Los triggers y restricciones de la base devuelven el motivo del rechazo
                    ModelState.AddModelError("", "No se pudo guardar: " + ex.GetBaseException().Message);
                }
            }
            ViewBag.EstadoAeronaveCodigo = new SelectList(db.EstadoAeronave, "Codigo", "Descripcion", aeronave.EstadoAeronaveCodigo);
            ViewBag.ModeloAeronaveId = new SelectList(db.ModeloAeronave, "ModeloAeronaveId", "Nombre", aeronave.ModeloAeronaveId);
            return View(aeronave);
        }

        // GET: Aeronaves/Delete/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Aeronave aeronave = db.Aeronave.Find(id);
            if (aeronave == null)
            {
                return HttpNotFound();
            }
            return View(aeronave);
        }

        // POST: Aeronaves/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult DeleteConfirmed(int id)
        {
            Aeronave aeronave = db.Aeronave.Find(id);
            db.Aeronave.Remove(aeronave);
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
