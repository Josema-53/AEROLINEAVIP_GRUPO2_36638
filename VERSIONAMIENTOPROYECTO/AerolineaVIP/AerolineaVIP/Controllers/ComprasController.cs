using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AerolineaVIP.Models;
using Microsoft.AspNet.Identity;

namespace AerolineaVIP.Controllers
{
    public class ComprasController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Compras/Pagar/5  (pantalla 11)
        [Authorize]
        public ActionResult Pagar(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ReglasReserva.ProcesarExpiraciones(db);
            Reserva reserva = db.Reserva.Find(id);
            if (reserva == null)
            {
                return HttpNotFound();
            }
            if (!ReglasReserva.PuedeOperar(reserva, User))
            {
                return View("AccesoDenegado");
            }
            if (reserva.EstadoReservaCodigo != "PENDIENTE")
            {
                TempData["Mensaje"] = "La reserva " + reserva.CodigoReserva + " ya no admite pagos (estado: " + reserva.EstadoReservaCodigo + ").";
                return RedirectToAction("Confirmacion", new { id = reserva.ReservaId });
            }

            var model = new PagoViewModel { MetodoPagoCodigo = "TARJETA_CREDITO" };
            CargarResumen(model, reserva);
            CargarListasPago();
            return View(model);
        }

        // POST: Compras/Pagar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Pagar(int id, PagoViewModel model)
        {
            ReglasReserva.ProcesarExpiraciones(db);
            Reserva reserva = db.Reserva.Find(id);
            if (reserva == null)
            {
                return HttpNotFound();
            }
            if (!ReglasReserva.PuedeOperar(reserva, User))
            {
                return View("AccesoDenegado");
            }

            // Solicitud repetida: si la compra ya está pagada se muestra el resultado existente (RNP-10)
            if (reserva.EstadoReservaCodigo != "PENDIENTE")
            {
                return RedirectToAction("Confirmacion", new { id = reserva.ReservaId });
            }

            // Validaciones del servidor: formato según la marca (no se verifica la tarjeta con ningún banco)
            var marca = db.MarcaTarjeta.FirstOrDefault(m => m.Codigo == model.MarcaTarjetaCodigo && m.Activa);
            if (marca == null)
            {
                ModelState.AddModelError("MarcaTarjetaCodigo", "Seleccione una marca de tarjeta válida.");
            }
            else
            {
                if (model.NumeroTarjeta != null && model.NumeroTarjeta.Length != marca.LongitudNumero)
                {
                    ModelState.AddModelError("NumeroTarjeta", "El número debe tener " + marca.LongitudNumero + " dígitos para " + marca.Nombre + ".");
                }
                if (model.CodigoSeguridad != null && model.CodigoSeguridad.Length != marca.LongitudCodigoSeguridad)
                {
                    ModelState.AddModelError("CodigoSeguridad", "El código de seguridad debe tener " + marca.LongitudCodigoSeguridad + " dígitos para " + marca.Nombre + ".");
                }
            }
            if (!db.MetodoPago.Any(m => m.Codigo == model.MetodoPagoCodigo && m.Activo))
            {
                ModelState.AddModelError("MetodoPagoCodigo", "Seleccione un método de pago válido.");
            }
            if (model.Vencimiento != null && ModelState.IsValidField("Vencimiento"))
            {
                int mes = int.Parse(model.Vencimiento.Substring(0, 2));
                int anio = 2000 + int.Parse(model.Vencimiento.Substring(3, 2));
                if (new DateTime(anio, mes, 1).AddMonths(1) <= DateTime.Today)
                {
                    ModelState.AddModelError("Vencimiento", "La tarjeta está vencida.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Por seguridad, el número y el código no se devuelven al formulario
                model.NumeroTarjeta = null;
                model.CodigoSeguridad = null;
                CargarResumen(model, reserva);
                CargarListasPago();
                return View(model);
            }

            // DP-06: los últimos 4 dígitos deciden el resultado de la simulación
            string sufijoRechazo = ConfigurationManager.AppSettings["SimPago.SufijoRechazo"] ?? "0000";
            string ultimos4 = model.NumeroTarjeta.Substring(model.NumeroTarjeta.Length - 4);
            bool aprobado = ultimos4 != sufijoRechazo;
            var ahora = DateTime.Now;

            using (var transaccion = db.Database.BeginTransaction())
            {
                try
                {
                    // Una sola compra vigente por reserva (UX_Compra_ReservaVigente)
                    var compra = reserva.Compra.FirstOrDefault(c => c.EstadoCompraCodigo == "PENDIENTE");
                    if (compra == null)
                    {
                        compra = new Compra
                        {
                            Reserva = reserva,
                            FechaCompra = ahora,
                            Importe = CalcularTotal(reserva),   // importe calculado en el servidor
                            MonedaCodigo = "USD",
                            EstadoCompraCodigo = "PENDIENTE",
                            ClaveIdempotencia = Guid.NewGuid()
                        };
                        db.Compra.Add(compra);
                    }

                    int intento = compra.Pago.Count + 1;
                    var pago = new Pago
                    {
                        Compra = compra,
                        FechaPago = ahora,
                        Importe = compra.Importe,
                        EstadoPagoCodigo = aprobado ? "APROBADO" : "RECHAZADO",
                        MetodoPagoCodigo = model.MetodoPagoCodigo,
                        MarcaTarjetaCodigo = model.MarcaTarjetaCodigo,
                        UltimosCuatroDigitos = ultimos4,       // nunca se guarda el número completo
                        NombreTitular = model.NombreTitular.Trim().ToUpper(),
                        CodigoOperacion = "SIM-" + reserva.CodigoReserva + "-" + intento.ToString("00"),
                        EsSimulado = true,
                        MensajeResultado = aprobado ? "Pago simulado aprobado." : "Pago simulado rechazado."
                    };
                    db.Pago.Add(pago);

                    if (aprobado)
                    {
                        // RG-06: compra pagada, reserva y asientos confirmados en una sola operación
                        compra.EstadoCompraCodigo = "PAGADA";
                        reserva.EstadoReservaCodigo = "CONFIRMADA";
                        foreach (var asignacion in reserva.ReservaPasajeroVuelo.Where(a => a.EstadoAsignacionCodigo == "PENDIENTE"))
                        {
                            asignacion.EstadoAsignacionCodigo = "CONFIRMADA";
                        }

                        // DP-05: comprobantes académicos (confirmación y comprobante de venta)
                        db.Comprobante.Add(new Comprobante
                        {
                            Compra = compra,
                            TipoComprobanteCodigo = "CONFIRMACION",
                            NumeroComprobante = ReglasReserva.SiguienteNumero(db, "CONFIRMACION", "CNF-"),
                            FechaEmision = ahora,
                            EstadoComprobanteCodigo = "EMITIDO"
                        });
                        db.Comprobante.Add(new Comprobante
                        {
                            Compra = compra,
                            TipoComprobanteCodigo = "FACTURA",
                            NumeroComprobante = ReglasReserva.SiguienteNumero(db, "FACTURA", "ACAD-"),
                            FechaEmision = ahora,
                            EstadoComprobanteCodigo = "EMITIDO"
                        });
                    }

                    db.SaveChanges();
                    transaccion.Commit();
                }
                catch (Exception)
                {
                    transaccion.Rollback();
                    TempData["Mensaje"] = "No se pudo registrar el pago simulado. Intente nuevamente.";
                    return RedirectToAction("Pagar", new { id = reserva.ReservaId });
                }
            }

            return RedirectToAction("Confirmacion", new { id = reserva.ReservaId });
        }

        // GET: Compras/Confirmacion/5  (pantalla 12)
        [Authorize]
        public ActionResult Confirmacion(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ReglasReserva.ProcesarExpiraciones(db);
            Reserva reserva = db.Reserva.Find(id);
            if (reserva == null)
            {
                return HttpNotFound();
            }
            if (!ReglasReserva.PuedeOperar(reserva, User))
            {
                return View("AccesoDenegado");
            }

            var compra = reserva.Compra.OrderByDescending(c => c.CompraId).FirstOrDefault();
            var ultimoPago = compra == null ? null : compra.Pago.OrderByDescending(p => p.PagoId).FirstOrDefault();

            var model = new ConfirmacionViewModel
            {
                ReservaId = reserva.ReservaId,
                CodigoReserva = reserva.CodigoReserva,
                EstadoReserva = reserva.EstadoReservaCodigo,
                EstadoCompra = compra == null ? "SIN COMPRA" : compra.EstadoCompraCodigo,
                EstadoPago = ultimoPago == null ? "SIN PAGO" : ultimoPago.EstadoPagoCodigo,
                CodigoOperacion = ultimoPago == null ? null : ultimoPago.CodigoOperacion,
                Total = compra != null ? compra.Importe : CalcularTotal(reserva),
                Metodo = ultimoPago == null ? null : ultimoPago.MetodoPago.Nombre + " · " + ultimoPago.MarcaTarjeta.Nombre + " **** " + ultimoPago.UltimosCuatroDigitos,
                Expira = reserva.FechaExpiracion,
                PuedePagar = reserva.EstadoReservaCodigo == "PENDIENTE",
                CompraPagada = compra != null && compra.EstadoCompraCodigo == "PAGADA"
            };

            // Mensaje según el resultado real persistido
            if (model.CompraPagada)
            {
                model.TipoMensaje = "success";
                model.Mensaje = "Compra " + reserva.CodigoReserva + " pagada. Pago simulado aprobado " + model.CodigoOperacion + ".";
            }
            else if (reserva.EstadoReservaCodigo == "PENDIENTE" && model.EstadoPago == "RECHAZADO")
            {
                model.TipoMensaje = "danger";
                model.Mensaje = "El pago simulado fue rechazado. La reserva sigue pendiente.";
            }
            else if (reserva.EstadoReservaCodigo == "PENDIENTE")
            {
                model.TipoMensaje = "info";
                model.Mensaje = "Reserva " + reserva.CodigoReserva + " registrada. La compra continúa pendiente de pago.";
            }
            else
            {
                model.TipoMensaje = "info";
                model.Mensaje = "La reserva " + reserva.CodigoReserva + " está en estado " + reserva.EstadoReservaCodigo + ".";
            }

            model.Detalle = ReglasReserva.CrearDetalle(db, reserva);
            return View(model);
        }

        // ----- Métodos auxiliares -----

        private decimal CalcularTotal(Reserva reserva)
        {
            return reserva.ReservaPasajeroVuelo
                .Where(a => a.EstadoAsignacionCodigo == "PENDIENTE" || a.EstadoAsignacionCodigo == "CONFIRMADA")
                .Sum(a => a.ImporteTramo);
        }

        private void CargarResumen(PagoViewModel model, Reserva reserva)
        {
            var detalle = ReglasReserva.CrearDetalle(db, reserva);
            var compra = reserva.Compra.FirstOrDefault(c => c.EstadoCompraCodigo == "PENDIENTE");

            model.ReservaId = reserva.ReservaId;
            model.CodigoReserva = reserva.CodigoReserva;
            model.Total = compra != null ? compra.Importe : CalcularTotal(reserva);
            model.Moneda = "USD";
            model.EstadoCompra = compra == null ? "Pendiente de pago" : "Pendiente de pago (intento " + (compra.Pago.Count + 1) + ")";
            model.Expira = reserva.FechaExpiracion;
            model.Pasajeros = detalle.Select(d => d.Pasajero).Distinct().Count();
            model.Asientos = string.Join(", ", detalle.Where(d => d.Tramo == 1).Select(d => d.Asiento));
            if (detalle.Count > 0)
            {
                var primero = detalle.First();
                var ultimo = detalle.Last();
                model.FechaViaje = primero.Fecha;
                model.RutaTexto = primero.Ruta.Split('→')[0].Trim() + " → " + ultimo.Ruta.Split('→')[1].Trim();
                model.Horario = primero.Horario.Split('–')[0] + " – " + ultimo.Horario.Split('–')[1]
                    + (detalle.Max(d => d.Tramo) > 1 ? " (" + detalle.Max(d => d.Tramo) + " tramos)" : "");
            }
        }

        private void CargarListasPago()
        {
            ViewBag.Metodos = db.MetodoPago.Where(m => m.Activo).ToList();
            ViewBag.Marcas = db.MarcaTarjeta.Where(m => m.Activa).ToList();
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
