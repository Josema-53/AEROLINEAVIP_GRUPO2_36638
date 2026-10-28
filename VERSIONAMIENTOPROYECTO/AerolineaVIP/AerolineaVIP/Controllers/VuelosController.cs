using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    public class VuelosController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Vuelos
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Index()
        {
            var query = db.Vuelo.Include("Aeronave").Include("EstadoVuelo")
                .Include("Frecuencia.Ruta.Aeropuerto").Include("Frecuencia.Ruta.Aeropuerto1")
                .Include("TramoItinerario.Itinerario").AsQueryable();

            // Filtros de búsqueda (fecha desde / hasta, ruta y estado)
            DateTime desde;
            if (DateTime.TryParse(Request.QueryString["desde"], out desde))
            {
                query = query.Where(v => v.FechaOperacion >= desde);
            }
            DateTime hasta;
            if (DateTime.TryParse(Request.QueryString["hasta"], out hasta))
            {
                query = query.Where(v => v.FechaOperacion <= hasta);
            }
            if (!string.IsNullOrEmpty(Request.QueryString["ruta"]))
            {
                int ruta = Convert.ToInt32(Request.QueryString["ruta"]);
                query = query.Where(v => v.Frecuencia.RutaId == ruta);
            }
            if (!string.IsNullOrEmpty(Request.QueryString["estado"]))
            {
                string estado = Request.QueryString["estado"];
                query = query.Where(v => v.EstadoVueloCodigo == estado);
            }

            var rutas = db.Ruta.Include("Aeropuerto").Include("Aeropuerto1").ToList()
                .Select(r => new { r.RutaId, Texto = r.Aeropuerto1.Localidad + " → " + r.Aeropuerto.Localidad });
            ViewBag.ruta = new SelectList(rutas, "RutaId", "Texto", Request.QueryString["ruta"]);
            ViewBag.estado = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion", Request.QueryString["estado"]);

            var vuelos = query.OrderBy(v => v.FechaOperacion).ThenBy(v => v.Frecuencia.HoraSalida).ToList();
            ViewBag.Ocupados = ReglasReserva.OcupadosPorVuelo(db, vuelos.Select(v => v.VueloId).ToList());
            return View(vuelos);
        }

        // GET: Vuelos/Details/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ReglasReserva.ProcesarExpiraciones(db);
            Vuelo vuelo = ReglasReserva.VuelosConDetalle(db).FirstOrDefault(v => v.VueloId == id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }

            // Mapa de asientos (misma vista que la pantalla 7, en modo consulta)
            ViewBag.Asientos = ReglasReserva.EstadoAsientos(db, vuelo);

            // Pasajeros con asiento vigente en el vuelo (solo visible para el personal administrativo)
            var estados = new[] { "PENDIENTE", "CONFIRMADA" };
            int vueloId = vuelo.VueloId;
            ViewBag.Asignaciones = db.ReservaPasajeroVuelo.Include("Pasajero").Include("Reserva")
                .Where(a => a.VueloId == vueloId && estados.Contains(a.EstadoAsignacionCodigo))
                .OrderBy(a => a.NumeroAsiento)
                .ToList();
            return View(vuelo);
        }

        // GET: Vuelos/Create
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create()
        {
            ViewBag.AeronaveId = new SelectList(db.Aeronave, "AeronaveId", "Matricula");
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion");
            ViewBag.FrecuenciaId = new SelectList(TextoFrecuencias(), "FrecuenciaId", "Texto");
            return View();
        }

        // POST: Vuelos/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create([Bind(Include = "VueloId,FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo vuelo)
        {
            string conflicto = ValidarAsignacion(vuelo);
            if (conflicto != null)
            {
                ModelState.AddModelError("AeronaveId", conflicto);
            }
            if (ModelState.IsValid)
            {
                db.Vuelo.Add(vuelo);
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

            ViewBag.AeronaveId = new SelectList(db.Aeronave, "AeronaveId", "Matricula", vuelo.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
            ViewBag.FrecuenciaId = new SelectList(TextoFrecuencias(), "FrecuenciaId", "Texto", vuelo.FrecuenciaId);
            return View(vuelo);
        }

        // GET: Vuelos/Edit/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vuelo.Find(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            ViewBag.AeronaveId = new SelectList(db.Aeronave, "AeronaveId", "Matricula", vuelo.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
            ViewBag.FrecuenciaId = new SelectList(TextoFrecuencias(), "FrecuenciaId", "Texto", vuelo.FrecuenciaId);
            return View(vuelo);
        }

        // POST: Vuelos/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit([Bind(Include = "VueloId,FrecuenciaId,FechaOperacion,AeronaveId,EstadoVueloCodigo")] Vuelo vuelo)
        {
            string conflicto = ValidarAsignacion(vuelo);
            if (conflicto != null)
            {
                ModelState.AddModelError("AeronaveId", conflicto);
            }
            if (ModelState.IsValid)
            {
                db.Entry(vuelo).State = EntityState.Modified;
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
            ViewBag.AeronaveId = new SelectList(db.Aeronave, "AeronaveId", "Matricula", vuelo.AeronaveId);
            ViewBag.EstadoVueloCodigo = new SelectList(db.EstadoVuelo, "Codigo", "Descripcion", vuelo.EstadoVueloCodigo);
            ViewBag.FrecuenciaId = new SelectList(TextoFrecuencias(), "FrecuenciaId", "Texto", vuelo.FrecuenciaId);
            return View(vuelo);
        }

        // GET: Vuelos/Delete/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Vuelo vuelo = db.Vuelo.Find(id);
            if (vuelo == null)
            {
                return HttpNotFound();
            }
            return View(vuelo);
        }

        // POST: Vuelos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult DeleteConfirmed(int id)
        {
            Vuelo vuelo = db.Vuelo.Find(id);
            db.Vuelo.Remove(vuelo);
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

        // ===================================================================================
        // PANTALLAS PÚBLICAS: búsqueda (5), resultados (6) y disponibilidad de asientos (7)
        // ===================================================================================

        // GET: Vuelos/Buscar
        public ActionResult Buscar(int? origenId, int? destinoId, DateTime? fecha, int? pasajeros)
        {
            var model = new BusquedaVuelosViewModel
            {
                OrigenId = origenId,
                DestinoId = destinoId,
                Fecha = fecha,
                Pasajeros = pasajeros ?? 1
            };
            CargarListasBusqueda(model);
            return View(model);
        }

        // GET: Vuelos/Resultados?OrigenId=1&DestinoId=2&Fecha=2026-11-03&Pasajeros=2
        public ActionResult Resultados(BusquedaVuelosViewModel model)
        {
            if (model.OrigenId != null && model.OrigenId == model.DestinoId)
            {
                ModelState.AddModelError("DestinoId", "El destino debe ser distinto del origen.");
            }
            if (model.Fecha != null && model.Fecha.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError("Fecha", "La fecha no puede ser anterior a hoy.");
            }
            if (!ModelState.IsValid)
            {
                CargarListasBusqueda(model);
                return View("Buscar", model);
            }

            ReglasReserva.ProcesarExpiraciones(db);

            var fechaVuelo = model.Fecha.Value.Date;
            int origenId = model.OrigenId.Value;
            int destinoId = model.DestinoId.Value;

            var vuelos = ReglasReserva.VuelosConDetalle(db).Where(v => v.FechaOperacion == fechaVuelo).ToList();
            var ocupados = ReglasReserva.OcupadosPorVuelo(db, vuelos.Select(v => v.VueloId).ToList());
            var opciones = new List<OpcionVueloViewModel>();

            // Vuelos directos
            foreach (var vuelo in vuelos.Where(v => v.Frecuencia.Ruta.AeropuertoOrigenId == origenId
                                                 && v.Frecuencia.Ruta.AeropuertoDestinoId == destinoId
                                                 && ReglasReserva.VentaIndividualPermitida(v)))
            {
                opciones.Add(CrearOpcion(new List<Vuelo> { vuelo }, ocupados, model.Pasajeros));
            }

            // Itinerarios con escala (DP-09 y DP-10): tramos contiguos de un mismo itinerario
            var tramos = db.TramoItinerario
                .Where(t => t.Itinerario.EstadoItinerarioCodigo == "ACTIVO" && t.Vuelo.FechaOperacion == fechaVuelo)
                .Select(t => new { t.ItinerarioId, t.Orden, t.VueloId })
                .ToList();

            foreach (var grupo in tramos.GroupBy(t => t.ItinerarioId))
            {
                var secuencia = grupo.OrderBy(t => t.Orden)
                    .Select(t => vuelos.FirstOrDefault(v => v.VueloId == t.VueloId))
                    .Where(v => v != null)
                    .ToList();

                for (int i = 0; i < secuencia.Count; i++)
                {
                    if (secuencia[i].Frecuencia.Ruta.AeropuertoOrigenId != origenId)
                    {
                        continue;
                    }
                    for (int j = i + 1; j < secuencia.Count; j++)
                    {
                        var anterior = secuencia[j - 1];
                        var siguiente = secuencia[j];
                        bool continuo = anterior.Frecuencia.Ruta.AeropuertoDestinoId == siguiente.Frecuencia.Ruta.AeropuertoOrigenId
                            && (ReglasReserva.Salida(siguiente) - ReglasReserva.Llegada(anterior)).TotalMinutes >= ReglasReserva.MinutosConexion;
                        if (!continuo)
                        {
                            break;
                        }
                        if (siguiente.Frecuencia.Ruta.AeropuertoDestinoId == destinoId)
                        {
                            opciones.Add(CrearOpcion(secuencia.GetRange(i, j - i + 1), ocupados, model.Pasajeros));
                            break;
                        }
                    }
                }
            }

            // Filtro opcional de horario (según la salida del primer tramo)
            if (model.Horario == "Manana")
            {
                opciones = opciones.Where(o => o.Tramos[0].HoraSalida.Hours < 12).ToList();
            }
            else if (model.Horario == "Tarde")
            {
                opciones = opciones.Where(o => o.Tramos[0].HoraSalida.Hours >= 12 && o.Tramos[0].HoraSalida.Hours < 18).ToList();
            }
            else if (model.Horario == "Noche")
            {
                opciones = opciones.Where(o => o.Tramos[0].HoraSalida.Hours >= 18).ToList();
            }

            var resultado = new ResultadosVuelosViewModel
            {
                Busqueda = model,
                OrigenTexto = TextoAeropuerto(origenId),
                DestinoTexto = TextoAeropuerto(destinoId),
                Opciones = opciones.OrderBy(o => o.Tramos[0].HoraSalida).ToList()
            };

            if (resultado.Opciones.Count == 0)
            {
                resultado.Mensaje = MensajeSinVuelos(origenId, destinoId);
            }
            return View(resultado);
        }

        // GET: Vuelos/Disponibilidad/31?pasajeros=2&vuelos=31,32
        public ActionResult Disponibilidad(int? id, int? pasajeros, string vuelos)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ReglasReserva.ProcesarExpiraciones(db);

            var model = new DisponibilidadViewModel
            {
                Pasajeros = Math.Min(7, Math.Max(1, pasajeros ?? 1)),
                VuelosIds = string.IsNullOrEmpty(vuelos) ? id.ToString() : vuelos,
                HoraCalculo = DateTime.Now
            };

            var vuelo = ReglasReserva.VuelosConDetalle(db).FirstOrDefault(v => v.VueloId == id);
            if (vuelo == null)
            {
                model.Mensaje = "El vuelo seleccionado no está disponible.";
                return View(model);
            }

            var ocupados = ReglasReserva.OcupadosPorVuelo(db, new List<int> { vuelo.VueloId });
            model.Vuelo = ReglasReserva.CrearTramo(vuelo, ocupados);
            model.Asientos = ReglasReserva.EstadoAsientos(db, vuelo);
            model.Mensaje = ReglasReserva.MotivoNoReservable(vuelo, model.Vuelo.Disponibles, model.Pasajeros);

            if (model.Mensaje == null && string.IsNullOrEmpty(vuelos) && !ReglasReserva.VentaIndividualPermitida(vuelo))
            {
                model.Mensaje = "Este tramo se vende únicamente como parte del itinerario a Galápagos.";
            }
            model.PuedeContinuar = model.Mensaje == null;
            return View(model);
        }

        // GET: Vuelos/DestinosPorOrigen?origenId=1 (usado por jQuery para filtrar el destino)
        public JsonResult DestinosPorOrigen(int origenId)
        {
            var destinos = ObtenerParesRuta()
                .Where(p => p.Item1 == origenId)
                .Select(p => p.Item2)
                .Distinct()
                .ToList();

            var lista = db.Aeropuerto
                .Where(a => destinos.Contains(a.AeropuertoId) && a.Activo)
                .OrderBy(a => a.Localidad)
                .ToList()
                .Select(a => new { id = a.AeropuertoId, nombre = a.Localidad + " (" + a.CodigoIATA + ")" });

            return Json(lista, JsonRequestBehavior.AllowGet);
        }

        // ----- Métodos auxiliares de las pantallas públicas -----

        private OpcionVueloViewModel CrearOpcion(List<Vuelo> tramos, Dictionary<int, int> ocupados, int pasajeros)
        {
            var opcion = new OpcionVueloViewModel();
            foreach (var vuelo in tramos)
            {
                var tramo = ReglasReserva.CrearTramo(vuelo, ocupados);
                opcion.Tramos.Add(tramo);
                if (opcion.Motivo == null)
                {
                    opcion.Motivo = ReglasReserva.MotivoNoReservable(vuelo, tramo.Disponibles, pasajeros);
                }
            }
            opcion.Comercializable = opcion.Motivo == null;
            return opcion;
        }

        // Pares origen-destino que se pueden buscar: rutas directas activas y tramos contiguos de itinerarios activos
        private List<Tuple<int, int>> ObtenerParesRuta()
        {
            var pares = db.Ruta
                .Where(r => r.Activa)
                .Select(r => new { r.AeropuertoOrigenId, r.AeropuertoDestinoId })
                .ToList()
                .Select(r => Tuple.Create(r.AeropuertoOrigenId, r.AeropuertoDestinoId))
                .ToList();

            var tramos = db.TramoItinerario
                .Where(t => t.Itinerario.EstadoItinerarioCodigo == "ACTIVO")
                .Select(t => new
                {
                    t.ItinerarioId,
                    t.Orden,
                    Origen = t.Vuelo.Frecuencia.Ruta.AeropuertoOrigenId,
                    Destino = t.Vuelo.Frecuencia.Ruta.AeropuertoDestinoId
                })
                .ToList();

            foreach (var grupo in tramos.GroupBy(t => t.ItinerarioId))
            {
                var secuencia = grupo.OrderBy(t => t.Orden).ToList();
                for (int i = 0; i < secuencia.Count; i++)
                {
                    for (int j = i + 1; j < secuencia.Count; j++)
                    {
                        if (secuencia[i].Origen != secuencia[j].Destino)
                        {
                            pares.Add(Tuple.Create(secuencia[i].Origen, secuencia[j].Destino));
                        }
                    }
                }
            }
            return pares.Distinct().ToList();
        }

        private void CargarListasBusqueda(BusquedaVuelosViewModel model)
        {
            var pares = ObtenerParesRuta();
            var origenes = pares.Select(p => p.Item1).Distinct().ToList();
            var destinos = model.OrigenId == null
                ? new List<int>()
                : pares.Where(p => p.Item1 == model.OrigenId).Select(p => p.Item2).Distinct().ToList();

            var aeropuertos = db.Aeropuerto.Where(a => a.Activo).OrderBy(a => a.Localidad).ToList();

            ViewBag.OrigenId = new SelectList(
                aeropuertos.Where(a => origenes.Contains(a.AeropuertoId))
                           .Select(a => new { a.AeropuertoId, Texto = a.Localidad + " (" + a.CodigoIATA + ")" }),
                "AeropuertoId", "Texto", model.OrigenId);

            ViewBag.DestinoId = new SelectList(
                aeropuertos.Where(a => destinos.Contains(a.AeropuertoId))
                           .Select(a => new { a.AeropuertoId, Texto = a.Localidad + " (" + a.CodigoIATA + ")" }),
                "AeropuertoId", "Texto", model.DestinoId);

            ViewBag.Horario = new SelectList(new[]
            {
                new { Valor = "", Texto = "Todos" },
                new { Valor = "Manana", Texto = "Mañana" },
                new { Valor = "Tarde", Texto = "Tarde" },
                new { Valor = "Noche", Texto = "Noche" }
            }, "Valor", "Texto", model.Horario);

            // Panel informativo con los días de operación de cada ruta
            var frecuencias = db.Frecuencia
                .Include(f => f.Ruta.Aeropuerto)
                .Include(f => f.Ruta.Aeropuerto1)
                .Where(f => f.Activa && f.Ruta.Activa)
                .ToList();

            var dias = new List<string>();
            foreach (var grupo in frecuencias.Where(f => f.DiaSemana != 7 || f.Ruta.Aeropuerto1.CodigoIATA == "GPS" || f.Ruta.Aeropuerto.CodigoIATA == "GPS")
                                             .GroupBy(f => f.RutaId))
            {
                var ruta = grupo.First().Ruta;
                var nombresDias = grupo.Select(f => (int)f.DiaSemana).Distinct().OrderBy(d => d).Select(d => ReglasReserva.NombreDia(d));
                dias.Add(ruta.Aeropuerto1.Localidad + " – " + ruta.Aeropuerto.Localidad + ": " + string.Join(", ", nombresDias));
            }
            if (frecuencias.Any(f => f.DiaSemana == 7 && f.Ruta.Aeropuerto.CodigoIATA == "GPS"))
            {
                dias.Add("Quito – Galápagos (escala en Guayaquil): domingo");
            }
            ViewBag.DiasOperacion = dias;
        }

        private string TextoAeropuerto(int aeropuertoId)
        {
            var aeropuerto = db.Aeropuerto.Find(aeropuertoId);
            return aeropuerto == null ? "" : aeropuerto.Localidad + " (" + aeropuerto.CodigoIATA + ")";
        }

        private string MensajeSinVuelos(int origenId, int destinoId)
        {
            if (!ObtenerParesRuta().Any(p => p.Item1 == origenId && p.Item2 == destinoId))
            {
                return "No existe una ruta entre el origen y el destino seleccionados.";
            }

            var diasRuta = db.Frecuencia
                .Where(f => f.Activa && f.Ruta.AeropuertoOrigenId == origenId && f.Ruta.AeropuertoDestinoId == destinoId)
                .Select(f => f.DiaSemana)
                .Distinct()
                .ToList()
                .Select(d => (int)d)
                .OrderBy(d => d)
                .ToList();

            if (diasRuta.Count > 0)
            {
                return "No hay vuelos para los criterios seleccionados. Esta ruta opera los días: "
                    + string.Join(", ", diasRuta.Select(d => ReglasReserva.NombreDia(d))) + ". Elija otra fecha.";
            }
            return "No hay vuelos para los criterios seleccionados.";
        }

        // ===================================================================================
        // PANTALLA 18: generación de vuelos, conflictos de aeronave y validación de asignaciones
        // ===================================================================================

        // POST: Vuelos/Generar  (ejecuta usp_GenerarVuelos para el rango; solo días autorizados)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Generar(DateTime? desdeGenerar, DateTime? hastaGenerar)
        {
            if (desdeGenerar == null || hastaGenerar == null || hastaGenerar < desdeGenerar)
            {
                TempData["Error"] = "Indique un rango de fechas válido para generar vuelos.";
                return RedirectToAction("Index");
            }
            if ((hastaGenerar.Value - desdeGenerar.Value).TotalDays > 365)
            {
                TempData["Error"] = "El rango no puede superar 366 días.";
                return RedirectToAction("Index");
            }

            try
            {
                int antes = db.Vuelo.Count();
                db.Database.ExecuteSqlCommand("EXEC dbo.usp_GenerarVuelos @FechaInicio, @FechaFin",
                    new SqlParameter("@FechaInicio", desdeGenerar.Value.Date),
                    new SqlParameter("@FechaFin", hastaGenerar.Value.Date));
                int generados = db.Vuelo.Count() - antes;
                TempData["Mensaje"] = generados == 0
                    ? "No se generaron vuelos nuevos: ya existían para ese rango."
                    : "Se generaron " + generados + " vuelos. Asígneles una aeronave para poder comercializarlos.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudieron generar los vuelos: " + ex.GetBaseException().Message;
            }
            return RedirectToAction("Index", new { desde = desdeGenerar.Value.ToString("yyyy-MM-dd"), hasta = hastaGenerar.Value.ToString("yyyy-MM-dd") });
        }

        // GET: Vuelos/Conflictos  (aeronaves con intervalos operativos incompatibles, DP-03)
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Conflictos()
        {
            var desde = DateTime.Today.AddDays(-7);
            var vuelos = ReglasReserva.VuelosConDetalle(db)
                .Where(v => v.AeronaveId != null && v.EstadoVueloCodigo != "CANCELADO" && v.FechaOperacion >= desde)
                .ToList();

            var conflictos = new List<string[]>();
            foreach (var grupo in vuelos.GroupBy(v => v.AeronaveId))
            {
                var lista = grupo.OrderBy(v => ReglasReserva.Salida(v)).ToList();
                for (int i = 0; i < lista.Count; i++)
                {
                    for (int j = i + 1; j < lista.Count; j++)
                    {
                        if (IntervalosIncompatibles(lista[i], lista[j]))
                        {
                            conflictos.Add(new[]
                            {
                                lista[i].Aeronave.Matricula,
                                DescribirVuelo(lista[i]),
                                DescribirVuelo(lista[j])
                            });
                        }
                    }
                }
            }
            ViewBag.Margen = MinutosMargen;
            return View(conflictos);
        }

        // DP-03: margen de preparación antes de la salida y después de la llegada (minutos)
        private const int MinutosMargen = 30;

        private bool IntervalosIncompatibles(Vuelo a, Vuelo b)
        {
            var inicioA = ReglasReserva.Salida(a).AddMinutes(-MinutosMargen);
            var finA = ReglasReserva.Llegada(a).AddMinutes(MinutosMargen);
            var inicioB = ReglasReserva.Salida(b).AddMinutes(-MinutosMargen);
            var finB = ReglasReserva.Llegada(b).AddMinutes(MinutosMargen);
            return inicioA < finB && inicioB < finA;   // RG-03 (fin exclusivo)
        }

        private string DescribirVuelo(Vuelo v)
        {
            return ReglasReserva.CodigoVuelo(v.VueloId) + " · " + v.FechaOperacion.ToString("dd/MM/yyyy") + " · "
                + v.Frecuencia.Ruta.Aeropuerto1.CodigoIATA + " → " + v.Frecuencia.Ruta.Aeropuerto.CodigoIATA + " "
                + v.Frecuencia.HoraSalida.ToString("hh\\:mm") + "–" + v.Frecuencia.HoraLlegada.ToString("hh\\:mm");
        }

        // Validación del servicio al asignar una aeronave (complementa el trigger trg_Vuelo_Validar)
        private string ValidarAsignacion(Vuelo vuelo)
        {
            if (vuelo.AeronaveId == null || vuelo.EstadoVueloCodigo == "CANCELADO")
            {
                return null;
            }
            var frecuencia = db.Frecuencia.Find(vuelo.FrecuenciaId);
            if (frecuencia == null)
            {
                return null;
            }
            var propuesto = new Vuelo { VueloId = vuelo.VueloId, FechaOperacion = vuelo.FechaOperacion, Frecuencia = frecuencia };

            var dia = vuelo.FechaOperacion.Date;
            var anterior = dia.AddDays(-1);
            var siguiente = dia.AddDays(1);
            int aeronaveId = vuelo.AeronaveId.Value;
            int vueloId = vuelo.VueloId;
            var otros = db.Vuelo.Include("Frecuencia").Include("Aeronave")
                .Where(v => v.AeronaveId == aeronaveId && v.VueloId != vueloId && v.EstadoVueloCodigo != "CANCELADO"
                            && v.FechaOperacion >= anterior && v.FechaOperacion <= siguiente)
                .AsNoTracking()
                .ToList();

            foreach (var otro in otros)
            {
                if (IntervalosIncompatibles(propuesto, otro))
                {
                    return "La aeronave " + otro.Aeronave.Matricula + " ya tiene el vuelo " + ReglasReserva.CodigoVuelo(otro.VueloId)
                        + " entre " + otro.Frecuencia.HoraSalida.ToString("hh\\:mm") + " y " + otro.Frecuencia.HoraLlegada.ToString("hh\\:mm")
                        + " (con " + MinutosMargen + " minutos de preparación). No se guardó la asignación.";
                }
            }
            return null;
        }

        private IEnumerable<object> TextoFrecuencias()
        {
            string[] dias = { "", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
            return db.Frecuencia.Include("Ruta.Aeropuerto").Include("Ruta.Aeropuerto1").ToList()
                .OrderBy(f => f.DiaSemana).ThenBy(f => f.HoraSalida)
                .Select(f => new
                {
                    f.FrecuenciaId,
                    Texto = dias[f.DiaSemana] + " " + f.HoraSalida.ToString("hh\\:mm") + " · " + f.Ruta.Aeropuerto1.CodigoIATA + " → " + f.Ruta.Aeropuerto.CodigoIATA
                });
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
