using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AerolineaVIP.Models;
using Microsoft.AspNet.Identity;

namespace AerolineaVIP.Controllers
{
    public class ComprobantesController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Comprobantes?desde=...&hasta=...&tipo=...&id=5  (pantalla 16)
        [Authorize]
        public ActionResult Index(int? id)
        {
            var query = ComprobantesPermitidos();

            // Filtros de búsqueda (fecha de emisión desde / hasta y tipo)
            DateTime desde;
            if (DateTime.TryParse(Request.QueryString["desde"], out desde))
            {
                query = query.Where(c => c.FechaEmision >= desde);
            }
            DateTime hasta;
            if (DateTime.TryParse(Request.QueryString["hasta"], out hasta))
            {
                DateTime finDia = hasta.AddDays(1);
                query = query.Where(c => c.FechaEmision < finDia);
            }
            if (!string.IsNullOrEmpty(Request.QueryString["tipo"]))
            {
                string tipo = Request.QueryString["tipo"];
                query = query.Where(c => c.TipoComprobanteCodigo == tipo);
            }

            ViewBag.tipo = new SelectList(db.TipoComprobante.ToList()
                .Select(t => new { t.Codigo, Texto = NombreTipo(t.Codigo) }), "Codigo", "Texto", Request.QueryString["tipo"]);

            var lista = query.OrderByDescending(c => c.FechaEmision).ToList()
                .Select(c => new FilaComprobante
                {
                    ComprobanteId = c.ComprobanteId,
                    Numero = c.NumeroComprobante,
                    Tipo = NombreTipo(c.TipoComprobanteCodigo),
                    Fecha = c.FechaEmision,
                    Reserva = c.Compra.Reserva.CodigoReserva,
                    Importe = c.TipoComprobanteCodigo == "CONFIRMACION" ? (decimal?)null : c.Compra.Importe,
                    Estado = c.EstadoComprobanteCodigo
                }).ToList();

            // Vista previa del comprobante seleccionado
            if (id != null)
            {
                var comprobante = ComprobantesPermitidos().FirstOrDefault(c => c.ComprobanteId == id);
                if (comprobante != null)
                {
                    ViewBag.Vista = CrearDetalle(comprobante);
                }
            }
            return View(lista);
        }

        // GET: Comprobantes/Imprimir/5  (versión para imprimir o guardar como PDF desde el navegador)
        [Authorize]
        public ActionResult Imprimir(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var comprobante = ComprobantesPermitidos().FirstOrDefault(c => c.ComprobanteId == id);
            if (comprobante == null)
            {
                return HttpNotFound();
            }
            return View(CrearDetalle(comprobante));
        }

        // ----- Métodos auxiliares -----

        // El cliente solo ve sus comprobantes; el personal ve todos
        private IQueryable<Comprobante> ComprobantesPermitidos()
        {
            var query = db.Comprobante.AsQueryable();
            if (!User.IsInRole("SuperUsuario") && !User.IsInRole("Administrador") && !User.IsInRole("Counter"))
            {
                string usuarioId = User.Identity.GetUserId();
                query = query.Where(c => c.Compra.Reserva.UsuarioId == usuarioId);
            }
            return query;
        }

        private static string NombreTipo(string codigo)
        {
            if (codigo == "FACTURA") { return "Comprobante de venta académico"; }
            if (codigo == "CONFIRMACION") { return "Confirmación"; }
            if (codigo == "NOTA_CREDITO") { return "Nota de crédito académica"; }
            return codigo;
        }

        // Comprobante generado solo con datos persistidos (RG-05)
        private ComprobanteDetalleViewModel CrearDetalle(Comprobante comprobante)
        {
            var compra = comprobante.Compra;
            var reserva = compra.Reserva;
            var emisor = db.DatosEmisor.FirstOrDefault();
            var pago = compra.Pago
                .OrderBy(p => p.EstadoPagoCodigo == "APROBADO" ? 0 : (p.EstadoPagoCodigo == "REVERSADO" ? 1 : 2))
                .ThenByDescending(p => p.FechaPago)
                .FirstOrDefault();

            // Datos del cliente desde la base de Identity
            string cliente;
            using (var identidad = new ApplicationDbContext())
            {
                var usuario = identidad.Users.FirstOrDefault(u => u.Id == reserva.UsuarioId);
                cliente = usuario == null ? "—"
                    : ((usuario.Nombres + " " + usuario.Apellidos).Trim() + " (" + usuario.UserName + ")");
            }

            var detalle = new ComprobanteDetalleViewModel
            {
                ComprobanteId = comprobante.ComprobanteId,
                Numero = comprobante.NumeroComprobante,
                Tipo = NombreTipo(comprobante.TipoComprobanteCodigo),
                FechaEmision = comprobante.FechaEmision,
                Estado = comprobante.EstadoComprobanteCodigo,
                EmisorNombre = emisor != null ? emisor.NombreComercial : "Aerolínea VIP",
                EmisorIdentificacion = emisor != null ? emisor.Identificacion : "",
                EmisorDireccion = emisor != null ? emisor.Direccion : "",
                Leyenda = emisor != null ? emisor.LeyendaComprobante
                    : "Comprobante académico de una operación simulada. No constituye factura tributaria ni documento autorizado.",
                Cliente = cliente,
                CodigoReserva = reserva.CodigoReserva,
                EstadoCompra = compra.EstadoCompraCodigo,
                Pago = pago == null ? "Sin pago registrado"
                    : pago.MarcaTarjeta.Nombre + " **** " + pago.UltimosCuatroDigitos + " · " + pago.CodigoOperacion + " · " + pago.EstadoPagoCodigo,
                Total = compra.Importe,
                Moneda = compra.MonedaCodigo
            };

            var asientos = db.Asiento.ToList();
            foreach (var a in reserva.ReservaPasajeroVuelo.OrderBy(x => ReglasReserva.Salida(x.Vuelo)).ThenBy(x => x.NumeroAsiento))
            {
                var vuelo = a.Vuelo;
                var asiento = vuelo.Aeronave == null ? null
                    : asientos.FirstOrDefault(s => s.ModeloAeronaveId == vuelo.Aeronave.ModeloAeronaveId && s.NumeroAsiento == a.NumeroAsiento);
                detalle.Lineas.Add(new LineaComprobante
                {
                    Pasajero = a.Pasajero.Nombres + " " + a.Pasajero.Apellidos,
                    Vuelo = ReglasReserva.CodigoVuelo(vuelo.VueloId),
                    Ruta = vuelo.Frecuencia.Ruta.Aeropuerto1.Localidad + " → " + vuelo.Frecuencia.Ruta.Aeropuerto.Localidad,
                    Fecha = vuelo.FechaOperacion,
                    Asiento = asiento != null ? asiento.Codigo : a.NumeroAsiento.ToString(),
                    Importe = a.ImporteTramo
                });
            }
            return detalle;
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
