using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    public class CheckInController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // DP-12: ventana de check-in (parámetros configurables en Web.config)
        private int HorasApertura
        {
            get { return int.Parse(ConfigurationManager.AppSettings["CheckIn.HorasApertura"] ?? "24"); }
        }

        private int MinutosCierre
        {
            get { return int.Parse(ConfigurationManager.AppSettings["CheckIn.MinutosCierre"] ?? "60"); }
        }

        // GET: CheckIn  (pantalla 15)
        [Authorize]
        public ActionResult Index(string codigo, string documento)
        {
            var model = new CheckInViewModel { Codigo = codigo, Documento = documento };
            if (TempData["Mensaje"] != null)
            {
                model.Mensaje = TempData["Mensaje"].ToString();
            }
            if (string.IsNullOrWhiteSpace(codigo) && string.IsNullOrWhiteSpace(documento))
            {
                return View(model);
            }

            model.Buscado = true;
            var reserva = BuscarReserva(codigo, documento);
            if (reserva == null)
            {
                // Mensaje neutral: no revela si la reserva existe
                model.Mensaje = "No se encontró una reserva con esos datos.";
                return View(model);
            }

            model.Filas = CrearFilas(reserva);
            return View(model);
        }

        // POST: CheckIn/Confirmar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Confirmar(string codigo, string documento, int[] asignaciones)
        {
            var reserva = BuscarReserva(codigo, documento);
            if (reserva == null)
            {
                TempData["Mensaje"] = "No se encontró una reserva con esos datos.";
                return RedirectToAction("Index");
            }
            if (asignaciones == null || asignaciones.Length == 0)
            {
                TempData["Mensaje"] = "Seleccione al menos un pasajero.";
                return RedirectToAction("Index", new { codigo, documento });
            }

            // Se vuelve a validar cada pasajero en el servidor (las mismas reglas que se muestran en pantalla)
            var filas = CrearFilas(reserva);
            var registrados = new List<string>();
            foreach (var id in asignaciones.Distinct())
            {
                var fila = filas.FirstOrDefault(f => f.AsignacionId == id);
                if (fila != null && fila.Elegible)
                {
                    db.CheckIn.Add(new CheckIn
                    {
                        AsignacionId = id,
                        FechaRegistro = DateTime.Now,
                        EstadoCheckInCodigo = "COMPLETADO"
                    });
                    registrados.Add(fila.Pasajero + " en " + fila.Vuelo);
                }
            }

            if (registrados.Count == 0)
            {
                TempData["Mensaje"] = "Ninguno de los pasajeros seleccionados es elegible para el check-in.";
                return RedirectToAction("Index", new { codigo, documento });
            }

            try
            {
                db.SaveChanges();
                TempData["Mensaje"] = "Check-in completado para " + string.Join("; ", registrados) + ".";
            }
            catch (Exception)
            {
                // El trigger de la base solo permite check-in de asientos confirmados (compra pagada)
                TempData["Mensaje"] = "El check-in solo puede completarse para reservas pagadas.";
            }
            return RedirectToAction("Index", new { codigo, documento });
        }

        // ----- Métodos auxiliares -----

        // La reserva se localiza con su código y el documento de uno de sus pasajeros.
        // El cliente solo accede a sus propias reservas; el personal de counter a cualquiera.
        private Reserva BuscarReserva(string codigo, string documento)
        {
            if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(documento))
            {
                return null;
            }
            codigo = codigo.Trim().ToUpper();
            documento = documento.Trim().ToUpper();

            var reserva = db.Reserva.FirstOrDefault(r => r.CodigoReserva == codigo);
            if (reserva == null || !ReglasReserva.PuedeOperar(reserva, User))
            {
                return null;
            }
            if (!reserva.ReservaPasajeroVuelo.Any(a => a.Pasajero.NumeroDocumento == documento))
            {
                return null;
            }
            return reserva;
        }

        private List<FilaCheckIn> CrearFilas(Reserva reserva)
        {
            var ahora = DateTime.Now;
            var asientos = db.Asiento.ToList();
            var filas = new List<FilaCheckIn>();

            var asignaciones = reserva.ReservaPasajeroVuelo
                .Where(a => a.EstadoAsignacionCodigo == "PENDIENTE" || a.EstadoAsignacionCodigo == "CONFIRMADA")
                .OrderBy(a => ReglasReserva.Salida(a.Vuelo)).ThenBy(a => a.NumeroAsiento);

            foreach (var a in asignaciones)
            {
                var vuelo = a.Vuelo;
                var salida = ReglasReserva.Salida(vuelo);
                var apertura = salida.AddHours(-HorasApertura);
                var cierre = salida.AddMinutes(-MinutosCierre);
                var checkIn = a.CheckIn.FirstOrDefault(c => c.EstadoCheckInCodigo == "COMPLETADO");
                var asiento = vuelo.Aeronave == null ? null
                    : asientos.FirstOrDefault(s => s.ModeloAeronaveId == vuelo.Aeronave.ModeloAeronaveId && s.NumeroAsiento == a.NumeroAsiento);

                var fila = new FilaCheckIn
                {
                    AsignacionId = a.AsignacionId,
                    Pasajero = a.Pasajero.Nombres + " " + a.Pasajero.Apellidos,
                    Documento = a.Pasajero.NumeroDocumento,
                    Vuelo = ReglasReserva.CodigoVuelo(vuelo.VueloId),
                    Ruta = vuelo.Frecuencia.Ruta.Aeropuerto1.Localidad + " → " + vuelo.Frecuencia.Ruta.Aeropuerto.Localidad,
                    Salida = salida,
                    Asiento = asiento != null ? asiento.Codigo : a.NumeroAsiento.ToString(),
                    Completado = checkIn != null,
                    FechaCheckIn = checkIn != null ? checkIn.FechaRegistro : (DateTime?)null
                };

                // RG-12: asiento confirmado (compra pagada) y ventana abierta [apertura, cierre)
                if (fila.Completado)
                {
                    fila.Elegibilidad = "Check-in completado";
                }
                else if (a.EstadoAsignacionCodigo != "CONFIRMADA")
                {
                    fila.Elegibilidad = "Requiere compra pagada";
                }
                else if (vuelo.EstadoVueloCodigo != "PROGRAMADO" && vuelo.EstadoVueloCodigo != "CONFIRMADO")
                {
                    fila.Elegibilidad = "Vuelo no disponible";
                }
                else if (ahora < apertura)
                {
                    fila.Elegibilidad = "Aún no habilitado (abre el " + apertura.ToString("dd/MM/yyyy HH:mm") + ")";
                }
                else if (ahora >= cierre)
                {
                    fila.Elegibilidad = "Check-in cerrado";
                }
                else
                {
                    fila.Elegibilidad = "Elegible";
                    fila.Elegible = true;
                }
                filas.Add(fila);
            }
            return filas;
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
