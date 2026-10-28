using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AerolineaVIP.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace AerolineaVIP.Controllers
{
    public class AdminController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Admin
        // Pantalla 17: accesos a los módulos administrativos (sin dashboard)
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Index()
        {
            return View();
        }

        // ===================================================================================
        // PANTALLA 20: usuarios (pestaña de Gestión de usuarios, pasajeros y reservas)
        // ===================================================================================

        // GET: Admin/Usuarios?buscar=...&rol=...
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Usuarios()
        {
            using (var identidad = new ApplicationDbContext())
            {
                var roles = identidad.Roles.ToList();
                var usuarios = identidad.Users.Include("Roles").ToList();

                // Filtros de búsqueda (usuario, correo o nombre; y rol)
                if (!string.IsNullOrEmpty(Request.QueryString["buscar"]))
                {
                    string buscar = Request.QueryString["buscar"].Trim().ToLower();
                    usuarios = usuarios.Where(u => (u.UserName ?? "").ToLower().Contains(buscar)
                                                || (u.Email ?? "").ToLower().Contains(buscar)
                                                || ((u.Nombres ?? "") + " " + (u.Apellidos ?? "")).ToLower().Contains(buscar)).ToList();
                }
                if (!string.IsNullOrEmpty(Request.QueryString["rol"]))
                {
                    var rol = roles.FirstOrDefault(r => r.Name == Request.QueryString["rol"]);
                    usuarios = rol == null ? new List<ApplicationUser>() : usuarios.Where(u => u.Roles.Any(r => r.RoleId == rol.Id)).ToList();
                }

                var filas = usuarios.OrderBy(u => u.UserName).Select(u => new string[]
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    ((u.Nombres ?? "") + " " + (u.Apellidos ?? "")).Trim(),
                    string.Join(", ", u.Roles.Select(r => roles.First(x => x.Id == r.RoleId).Name)),
                    u.LockoutEndDateUtc != null && u.LockoutEndDateUtc > DateTime.UtcNow ? "Bloqueado" : "Activo"
                }).ToList();

                ViewBag.rol = new SelectList(roles.Select(r => r.Name).OrderBy(n => n).ToList(), Request.QueryString["rol"]);
                ViewBag.Roles = new[] { "Cliente", "Counter", "Administrador", "SuperUsuario" };
                return View(filas);
            }
        }

        // POST: Admin/AsignarRol  (solo el SuperUsuario asigna roles)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario")]
        public ActionResult AsignarRol(string usuarioId, string rol)
        {
            string[] permitidos = { "Cliente", "Counter", "Administrador", "SuperUsuario" };
            if (!permitidos.Contains(rol))
            {
                TempData["Error"] = "Rol no válido.";
                return RedirectToAction("Usuarios");
            }
            if (usuarioId == User.Identity.GetUserId())
            {
                TempData["Error"] = "No puede cambiar su propio rol.";
                return RedirectToAction("Usuarios");
            }

            using (var identidad = new ApplicationDbContext())
            {
                var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identidad));
                var usuario = usermanager.FindById(usuarioId);
                if (usuario == null)
                {
                    TempData["Error"] = "No se encontró el usuario.";
                    return RedirectToAction("Usuarios");
                }

                // Cada usuario tiene un único rol: se quitan los anteriores y se asigna el nuevo
                foreach (var anterior in usermanager.GetRoles(usuarioId).ToList())
                {
                    usermanager.RemoveFromRole(usuarioId, anterior);
                }
                usermanager.AddToRole(usuarioId, rol);
                TempData["Mensaje"] = "Se asignó el rol " + rol + " a " + usuario.UserName + ".";
            }
            return RedirectToAction("Usuarios");
        }

        // ===================================================================================
        // PANTALLA 21: consultas y reportes administrativos (tablas, sin dashboard)
        // ===================================================================================

        // GET: Admin/Reportes?consulta=...&desde=...&hasta=...&ruta=...&aeronave=...&estado=...
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Reportes()
        {
            string consulta = Request.QueryString["consulta"] ?? "";
            ViewBag.consulta = new SelectList(new[]
            {
                new { Valor = "OCUPACION", Texto = "Ocupación por vuelo" },
                new { Valor = "RESERVAS", Texto = "Reservas por estado" },
                new { Valor = "PAGOS", Texto = "Compras y pagos simulados" }
            }, "Valor", "Texto", consulta);
            ViewBag.ruta = new SelectList(db.Ruta.Include("Aeropuerto").Include("Aeropuerto1").ToList()
                .Select(r => new { r.RutaId, Texto = r.Aeropuerto1.Localidad + " → " + r.Aeropuerto.Localidad }), "RutaId", "Texto", Request.QueryString["ruta"]);
            ViewBag.aeronave = new SelectList(db.Aeronave, "AeronaveId", "Matricula", Request.QueryString["aeronave"]);
            ViewBag.ConsultaActual = consulta;
            ViewBag.Corte = DateTime.Now;

            if (consulta == "")
            {
                return View();
            }

            // Validación del rango de fechas (obligatorio, máximo 366 días)
            DateTime desde, hasta;
            if (!DateTime.TryParse(Request.QueryString["desde"], out desde) || !DateTime.TryParse(Request.QueryString["hasta"], out hasta))
            {
                ViewBag.Error = "Indique la fecha desde y la fecha hasta.";
                return View();
            }
            if (hasta < desde || (hasta - desde).TotalDays > 365)
            {
                ViewBag.Error = "El rango de fechas no es válido (máximo 366 días).";
                return View();
            }

            ReglasReserva.ProcesarExpiraciones(db);
            string estado = Request.QueryString["estado"];
            var finDia = hasta.AddDays(1);

            if (consulta == "OCUPACION")
            {
                // Disponibles = Capacidad − (asientos reservados u ocupados)
                var query = ReglasReserva.VuelosConDetalle(db).Where(v => v.FechaOperacion >= desde && v.FechaOperacion <= hasta);
                if (!string.IsNullOrEmpty(Request.QueryString["ruta"]))
                {
                    int ruta = Convert.ToInt32(Request.QueryString["ruta"]);
                    query = query.Where(v => v.Frecuencia.RutaId == ruta);
                }
                if (!string.IsNullOrEmpty(Request.QueryString["aeronave"]))
                {
                    int aeronave = Convert.ToInt32(Request.QueryString["aeronave"]);
                    query = query.Where(v => v.AeronaveId == aeronave);
                }
                if (!string.IsNullOrEmpty(estado))
                {
                    query = query.Where(v => v.EstadoVueloCodigo == estado);
                }
                var vuelos = query.ToList().OrderBy(v => ReglasReserva.Salida(v)).ToList();
                var ocupados = ReglasReserva.OcupadosPorVuelo(db, vuelos.Select(v => v.VueloId).ToList());
                ViewBag.Columnas = new[] { "Vuelo", "Fecha", "Ruta", "Aeronave", "Capacidad", "Ocupados", "Disponibles", "Estado" };
                ViewBag.Filas = vuelos.Select(v =>
                {
                    var t = ReglasReserva.CrearTramo(v, ocupados);
                    return new[]
                    {
                        t.CodigoVuelo, t.Fecha.ToString("dd/MM/yyyy"), t.Origen + " → " + t.Destino,
                        v.Aeronave != null ? v.Aeronave.Matricula : "Sin asignar",
                        t.Capacidad.ToString(), (t.Capacidad - t.Disponibles).ToString(), t.Disponibles.ToString(), v.EstadoVueloCodigo
                    };
                }).ToList();
                ViewBag.Estados = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion", estado);
            }
            else if (consulta == "RESERVAS")
            {
                var reservas = db.Reserva.Include("ReservaPasajeroVuelo")
                    .Where(r => r.FechaCreacion >= desde && r.FechaCreacion < finDia).ToList();
                if (!string.IsNullOrEmpty(estado))
                {
                    reservas = reservas.Where(r => r.EstadoReservaCodigo == estado).ToList();
                }
                ViewBag.Columnas = new[] { "Estado de la reserva", "Reservas", "Asientos", "Importe total (USD)" };
                ViewBag.Filas = reservas.GroupBy(r => r.EstadoReservaCodigo).OrderBy(g => g.Key).Select(g => new[]
                {
                    g.Key,
                    g.Count().ToString(),
                    g.Sum(r => r.ReservaPasajeroVuelo.Count).ToString(),
                    g.Sum(r => r.ReservaPasajeroVuelo.Sum(a => a.ImporteTramo)).ToString("N2")
                }).ToList();
                ViewBag.Estados = new SelectList(db.EstadoReserva, "Codigo", "Descripcion", estado);
            }
            else if (consulta == "PAGOS")
            {
                var pagos = db.Pago.Include("Compra.Reserva").Include("MarcaTarjeta")
                    .Where(p => p.FechaPago >= desde && p.FechaPago < finDia).ToList();
                if (!string.IsNullOrEmpty(estado))
                {
                    pagos = pagos.Where(p => p.EstadoPagoCodigo == estado).ToList();
                }
                ViewBag.Columnas = new[] { "Operación", "Reserva", "Fecha", "Tarjeta", "Importe (USD)", "Pago simulado", "Compra" };
                ViewBag.Filas = pagos.OrderBy(p => p.FechaPago).Select(p => new[]
                {
                    p.CodigoOperacion, p.Compra.Reserva.CodigoReserva, p.FechaPago.ToString("dd/MM/yyyy HH:mm"),
                    (p.MarcaTarjeta != null ? p.MarcaTarjeta.Nombre : p.MetodoPagoCodigo) + " **** " + p.UltimosCuatroDigitos, p.Importe.ToString("N2"),
                    p.EstadoPagoCodigo, p.Compra.EstadoCompraCodigo
                }).ToList();
                ViewBag.Estados = new SelectList(db.EstadoPago, "Codigo", "Descripcion", estado);
            }
            return View();
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
