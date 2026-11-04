using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    [Authorize(Roles = "Counter,Administrador,SuperUsuario")]
    public class CounterController : Controller
    {
        private static readonly object candadoAuditoria = new object();
        private readonly AerolineaVIPContext db = new AerolineaVIPContext();

        public ActionResult Index(string codigo, string documento)
        {
            ReglasReserva.ProcesarExpiraciones(db);
            var model = new CounterAtencionViewModel
            {
                Codigo = (codigo ?? "").Trim().ToUpperInvariant(),
                Documento = (documento ?? "").Trim().ToUpperInvariant(),
                Mensaje = TempData["Mensaje"] as string
            };
            if (string.IsNullOrWhiteSpace(codigo) && string.IsNullOrWhiteSpace(documento)) return View(model);
            model.Buscado = true;
            if (string.IsNullOrWhiteSpace(model.Codigo) || string.IsNullOrWhiteSpace(model.Documento))
            {
                model.Mensaje = "Ingrese el código de reserva y el documento de uno de sus pasajeros.";
                return View(model);
            }
            var reserva = CargarReserva(model.Codigo);
            if (reserva == null || !reserva.ReservaPasajeroVuelo.Any(a => a.Pasajero.NumeroDocumento == model.Documento))
            {
                model.Mensaje = "No se encontró una reserva con esos datos.";
                return View(model);
            }
            model.Encontrada = true;
            model.ReservaId = reserva.ReservaId;
            model.EstadoReserva = reserva.EstadoReservaCodigo;
            model.Asignaciones = CrearAsignaciones(reserva);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarAsiento(int reservaId, int asignacionId, byte nuevoAsiento, string codigo, string documento)
        {
            ReglasReserva.ProcesarExpiraciones(db);
            codigo = (codigo ?? "").Trim().ToUpperInvariant();
            documento = (documento ?? "").Trim().ToUpperInvariant();
            var reserva = db.Reserva.Find(reservaId);
            if (reserva == null) return HttpNotFound();
            var asignacion = db.ReservaPasajeroVuelo.Find(asignacionId);
            if (reserva.CodigoReserva != codigo || asignacion == null || asignacion.ReservaId != reservaId
                || !db.ReservaPasajeroVuelo.Any(a => a.ReservaId == reservaId && a.Pasajero.NumeroDocumento == documento))
            {
                TempData["Mensaje"] = "No se pudo validar la reserva y el pasajero.";
                return Volver(codigo, documento);
            }
            if ((reserva.EstadoReservaCodigo != "PENDIENTE" && reserva.EstadoReservaCodigo != "CONFIRMADA")
                || (asignacion.EstadoAsignacionCodigo != "PENDIENTE" && asignacion.EstadoAsignacionCodigo != "CONFIRMADA"))
            {
                TempData["Mensaje"] = "Solo se pueden modificar asientos activos de reservas pendientes o confirmadas.";
                return Volver(codigo, documento);
            }
            if (asignacion.CheckIn.Any(c => c.EstadoCheckInCodigo == "COMPLETADO") || ReglasReserva.Salida(asignacion.Vuelo) <= DateTime.Now)
            {
                TempData["Mensaje"] = "No se puede cambiar el asiento después del check-in o de la salida del vuelo.";
                return Volver(codigo, documento);
            }
            if (asignacion.NumeroAsiento == nuevoAsiento)
            {
                TempData["Mensaje"] = "Seleccione un asiento distinto al actual.";
                return Volver(codigo, documento);
            }
            var aeronave = asignacion.Vuelo.Aeronave;
            if (aeronave == null || !db.Asiento.Any(s => s.ModeloAeronaveId == aeronave.ModeloAeronaveId && s.NumeroAsiento == nuevoAsiento))
            {
                TempData["Mensaje"] = "El asiento seleccionado no existe en la aeronave asignada al vuelo.";
                return Volver(codigo, documento);
            }

            byte anterior = asignacion.NumeroAsiento;
            using (var transaccion = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var activos = new[] { "PENDIENTE", "CONFIRMADA" };
                    bool checkInCompletado = db.CheckIn.Any(c => c.AsignacionId == asignacionId && c.EstadoCheckInCodigo == "COMPLETADO");
                    bool reservaModificable = reserva.EstadoReservaCodigo == "PENDIENTE" || reserva.EstadoReservaCodigo == "CONFIRMADA";
                    bool asignacionActiva = activos.Contains(asignacion.EstadoAsignacionCodigo);
                    if (checkInCompletado || !reservaModificable || !asignacionActiva || ReglasReserva.Salida(asignacion.Vuelo) <= DateTime.Now)
                    {
                        transaccion.Rollback();
                        TempData["Mensaje"] = "La reserva dejó de ser modificable. Consulte nuevamente su estado.";
                        return Volver(codigo, documento);
                    }
                    bool ocupado = db.ReservaPasajeroVuelo.Any(a => a.VueloId == asignacion.VueloId
                        && a.NumeroAsiento == nuevoAsiento && a.AsignacionId != asignacionId && activos.Contains(a.EstadoAsignacionCodigo));
                    if (ocupado)
                    {
                        transaccion.Rollback();
                        TempData["Mensaje"] = "Ese asiento acaba de ser ocupado. Seleccione otro.";
                        return Volver(codigo, documento);
                    }
                    asignacion.NumeroAsiento = nuevoAsiento;
                    db.SaveChanges();
                    RegistrarCambio(reservaId, asignacionId, asignacion.VueloId, anterior, nuevoAsiento);
                    transaccion.Commit();
                    TempData["Mensaje"] = "Asiento cambiado correctamente. El cambio quedó registrado.";
                }
                catch (DbUpdateException)
                {
                    transaccion.Rollback();
                    db.Entry(asignacion).Reload();
                    TempData["Mensaje"] = "No se pudo cambiar el asiento porque dejó de estar disponible. Busque la reserva nuevamente.";
                }
                catch (IOException)
                {
                    transaccion.Rollback();
                    db.Entry(asignacion).Reload();
                    TempData["Mensaje"] = "No se guardó el cambio porque no se pudo registrar la auditoría. Verifique permisos de escritura en App_Data.";
                }
                catch (UnauthorizedAccessException)
                {
                    transaccion.Rollback();
                    db.Entry(asignacion).Reload();
                    TempData["Mensaje"] = "No se guardó el cambio porque la aplicación no tiene permiso para escribir la auditoría en App_Data.";
                }
            }
            return Volver(codigo, documento);
        }

        private ActionResult Volver(string codigo, string documento)
        {
            return RedirectToAction("Index", new { codigo, documento });
        }

        private Reserva CargarReserva(string codigo)
        {
            return db.Reserva
                .Include("ReservaPasajeroVuelo.Pasajero")
                .Include("ReservaPasajeroVuelo.Vuelo.Frecuencia.Ruta.Aeropuerto")
                .Include("ReservaPasajeroVuelo.Vuelo.Frecuencia.Ruta.Aeropuerto1")
                .Include("ReservaPasajeroVuelo.Vuelo.Aeronave")
                .Include("ReservaPasajeroVuelo.CheckIn")
                .FirstOrDefault(r => r.CodigoReserva == codigo);
        }

        private List<CounterAsignacionViewModel> CrearAsignaciones(Reserva reserva)
        {
            var activos = new[] { "PENDIENTE", "CONFIRMADA" };
            var filas = new List<CounterAsignacionViewModel>();
            foreach (var a in reserva.ReservaPasajeroVuelo.OrderBy(x => ReglasReserva.Salida(x.Vuelo)).ThenBy(x => x.NumeroAsiento))
            {
                var vuelo = a.Vuelo;
                var ruta = vuelo.Frecuencia.Ruta;
                bool permiteCambio = (reserva.EstadoReservaCodigo == "PENDIENTE" || reserva.EstadoReservaCodigo == "CONFIRMADA")
                    && (a.EstadoAsignacionCodigo == "PENDIENTE" || a.EstadoAsignacionCodigo == "CONFIRMADA")
                    && !a.CheckIn.Any(c => c.EstadoCheckInCodigo == "COMPLETADO")
                    && ReglasReserva.Salida(vuelo) > DateTime.Now && vuelo.Aeronave != null;
                int modeloId = vuelo.Aeronave == null ? 0 : vuelo.Aeronave.ModeloAeronaveId;
                var asiento = db.Asiento.FirstOrDefault(s => s.ModeloAeronaveId == modeloId && s.NumeroAsiento == a.NumeroAsiento);
                var opciones = new List<SelectListItem>();
                if (permiteCambio)
                {
                    var ocupados = db.ReservaPasajeroVuelo.Where(x => x.VueloId == vuelo.VueloId && x.AsignacionId != a.AsignacionId
                        && activos.Contains(x.EstadoAsignacionCodigo)).Select(x => x.NumeroAsiento).ToList();
                    opciones = db.Asiento.Where(s => s.ModeloAeronaveId == modeloId).OrderBy(s => s.NumeroAsiento).ToList()
                        .Select(s => new SelectListItem { Value = s.NumeroAsiento.ToString(), Text = s.Codigo,
                            Selected = s.NumeroAsiento == a.NumeroAsiento, Disabled = s.NumeroAsiento != a.NumeroAsiento && ocupados.Contains(s.NumeroAsiento) }).ToList();
                }
                filas.Add(new CounterAsignacionViewModel
                {
                    AsignacionId = a.AsignacionId,
                    Pasajero = a.Pasajero.Nombres + " " + a.Pasajero.Apellidos,
                    Vuelo = ReglasReserva.CodigoVuelo(vuelo.VueloId),
                    Ruta = ruta.Aeropuerto1.Localidad + " → " + ruta.Aeropuerto.Localidad,
                    Salida = ReglasReserva.Salida(vuelo),
                    AsientoActual = asiento == null ? a.NumeroAsiento.ToString() : asiento.Codigo,
                    Estado = a.EstadoAsignacionCodigo,
                    PuedeCambiarAsiento = permiteCambio,
                    AsientosDisponibles = opciones
                });
            }
            return filas;
        }

        private void RegistrarCambio(int reservaId, int asignacionId, int vueloId, byte anterior, byte nuevo)
        {
            string archivo = Server.MapPath("~/App_Data/CounterCambios.log");
            string entrada = string.Format("{0:yyyy-MM-dd HH:mm:ss}\tUsuario={1}\tReservaId={2}\tAsignacionId={3}\tVueloId={4}\tAsientoAnterior={5}\tAsientoNuevo={6}{7}",
                DateTime.Now, User.Identity.Name ?? "usuario", reservaId, asignacionId, vueloId, anterior, nuevo, Environment.NewLine);
            lock (candadoAuditoria) { System.IO.File.AppendAllText(archivo, entrada); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
