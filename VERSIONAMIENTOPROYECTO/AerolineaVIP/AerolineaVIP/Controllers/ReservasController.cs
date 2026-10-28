using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AerolineaVIP.Models;
using Microsoft.AspNet.Identity;

namespace AerolineaVIP.Controllers
{
    public class ReservasController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // GET: Reservas
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Index()
        {
            var reserva = db.Reserva.Include(r => r.EstadoReserva);
            return View(reserva.ToList());
        }

        // GET: Reservas/Details/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Reserva reserva = db.Reserva.Find(id);
            if (reserva == null)
            {
                return HttpNotFound();
            }
            return View(reserva);
        }

        // GET: Reservas/Create
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create()
        {
            ViewBag.EstadoReservaCodigo = new SelectList(db.EstadoReserva, "Codigo", "Descripcion");
            return View();
        }

        // POST: Reservas/Create
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Create([Bind(Include = "ReservaId,CodigoReserva,UsuarioId,FechaCreacion,FechaExpiracion,EstadoReservaCodigo")] Reserva reserva)
        {
            if (ModelState.IsValid)
            {
                db.Reserva.Add(reserva);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.EstadoReservaCodigo = new SelectList(db.EstadoReserva, "Codigo", "Descripcion", reserva.EstadoReservaCodigo);
            return View(reserva);
        }

        // GET: Reservas/Edit/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Reserva reserva = db.Reserva.Find(id);
            if (reserva == null)
            {
                return HttpNotFound();
            }
            ViewBag.EstadoReservaCodigo = new SelectList(db.EstadoReserva, "Codigo", "Descripcion", reserva.EstadoReservaCodigo);
            return View(reserva);
        }

        // POST: Reservas/Edit/5
        // Para protegerse de ataques de publicación excesiva, habilite las propiedades específicas a las que quiere enlazarse. Para obtener 
        // más detalles, vea https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Edit([Bind(Include = "ReservaId,CodigoReserva,UsuarioId,FechaCreacion,FechaExpiracion,EstadoReservaCodigo")] Reserva reserva)
        {
            if (ModelState.IsValid)
            {
                db.Entry(reserva).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.EstadoReservaCodigo = new SelectList(db.EstadoReserva, "Codigo", "Descripcion", reserva.EstadoReservaCodigo);
            return View(reserva);
        }

        // GET: Reservas/Delete/5
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Reserva reserva = db.Reserva.Find(id);
            if (reserva == null)
            {
                return HttpNotFound();
            }
            return View(reserva);
        }

        // POST: Reservas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperUsuario,Administrador")]
        public ActionResult DeleteConfirmed(int id)
        {
            Reserva reserva = db.Reserva.Find(id);
            db.Reserva.Remove(reserva);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // ===================================================================================
        // FLUJO DE RESERVA: selección de asientos (8), pasajeros (9) y confirmación (10)
        // La operación en curso se guarda en Session["Operacion"] hasta confirmar la reserva.
        // ===================================================================================

        // GET: Reservas/SeleccionAsiento?vuelos=31,32&pasajeros=2
        [Authorize]
        public ActionResult SeleccionAsiento(string vuelos, int? pasajeros)
        {
            var ids = LeerVuelos(vuelos);
            int cantidad = Math.Min(7, Math.Max(1, pasajeros ?? 1));

            ReglasReserva.ProcesarExpiraciones(db);
            var listaVuelos = CargarVuelos(ids);
            string error = ValidarVuelos(listaVuelos, cantidad);
            if (error != null)
            {
                TempData["Mensaje"] = error;
                return RedirectToAction("Buscar", "Vuelos");
            }

            // Si se regresa desde la pantalla 9 con la misma operación, se conservan los asientos elegidos
            var operacion = Session["Operacion"] as OperacionReserva;
            if (operacion == null || operacion.VuelosTexto != string.Join(",", ids) || operacion.CantidadPasajeros != cantidad)
            {
                operacion = new OperacionReserva { VueloIds = ids, CantidadPasajeros = cantidad };
                Session["Operacion"] = operacion;
            }

            return View(CrearModeloSeleccion(listaVuelos, operacion, null));
        }

        // POST: Reservas/SeleccionAsiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult SeleccionAsiento(string selecciones)
        {
            var operacion = Session["Operacion"] as OperacionReserva;
            if (operacion == null)
            {
                TempData["Mensaje"] = "La selección expiró. Vuelva a buscar el vuelo.";
                return RedirectToAction("Buscar", "Vuelos");
            }

            ReglasReserva.ProcesarExpiraciones(db);
            var listaVuelos = CargarVuelos(operacion.VueloIds);

            // Formato recibido: "pasajero-vuelo-numeroAsiento" separados por comas (lo arma jQuery)
            var elegidos = new List<SeleccionAsientoItem>();
            foreach (var parte in (selecciones ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var datos = parte.Split('-');
                int p, v;
                byte n;
                if (datos.Length == 3 && int.TryParse(datos[0], out p) && int.TryParse(datos[1], out v) && byte.TryParse(datos[2], out n))
                {
                    elegidos.Add(new SeleccionAsientoItem { Pasajero = p, VueloId = v, NumeroAsiento = n });
                }
            }

            // Validaciones del servidor (RNP-04): un asiento válido y libre por pasajero y tramo
            var validos = new List<SeleccionAsientoItem>();
            var errores = new List<string>();
            foreach (var vuelo in listaVuelos)
            {
                var asientos = ReglasReserva.EstadoAsientos(db, vuelo);
                for (int p = 0; p < operacion.CantidadPasajeros; p++)
                {
                    var eleccion = elegidos.FirstOrDefault(e => e.Pasajero == p && e.VueloId == vuelo.VueloId);
                    if (eleccion == null)
                    {
                        errores.Add("Falta asignar asiento al Pasajero " + (p + 1) + " en el vuelo " + ReglasReserva.CodigoVuelo(vuelo.VueloId) + ".");
                        continue;
                    }
                    var asiento = asientos.FirstOrDefault(a => a.NumeroAsiento == eleccion.NumeroAsiento);
                    if (asiento == null)
                    {
                        errores.Add("El asiento elegido no existe en la aeronave.");
                    }
                    else if (asiento.Estado != "DISPONIBLE")
                    {
                        errores.Add("El asiento " + asiento.Codigo + " acaba de ser ocupado. Seleccione otro.");
                    }
                    else if (validos.Any(x => x.VueloId == vuelo.VueloId && x.NumeroAsiento == eleccion.NumeroAsiento))
                    {
                        errores.Add("El asiento " + asiento.Codigo + " está elegido para dos pasajeros.");
                    }
                    else
                    {
                        eleccion.Codigo = asiento.Codigo;
                        validos.Add(eleccion);
                    }
                }
            }

            operacion.Asientos = validos;
            Session["Operacion"] = operacion;

            if (errores.Count > 0)
            {
                // Se redibuja el mapa con el estado real y se conservan las selecciones válidas
                return View(CrearModeloSeleccion(listaVuelos, operacion, string.Join(" ", errores.Distinct())));
            }
            return RedirectToAction("Pasajeros");
        }

        // GET: Reservas/Pasajeros
        [Authorize]
        public ActionResult Pasajeros()
        {
            var operacion = Session["Operacion"] as OperacionReserva;
            if (operacion == null || !operacion.AsientosCompletos)
            {
                TempData["Mensaje"] = "Primero seleccione los asientos.";
                return RedirectToAction("Buscar", "Vuelos");
            }

            var model = CrearModeloPasajeros(operacion);
            if (operacion.Pasajeros != null)
            {
                model.Pasajeros = operacion.Pasajeros;
            }
            else
            {
                for (int i = 0; i < operacion.CantidadPasajeros; i++)
                {
                    model.Pasajeros.Add(new PasajeroFormViewModel { TipoDocumentoCodigo = "CEDULA" });
                }
            }
            ViewBag.TiposDocumento = db.TipoDocumentoIdentidad.ToList();
            return View(model);
        }

        // POST: Reservas/Pasajeros
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Pasajeros(PasajerosViewModel model)
        {
            var operacion = Session["Operacion"] as OperacionReserva;
            if (operacion == null || !operacion.AsientosCompletos)
            {
                TempData["Mensaje"] = "La selección expiró. Vuelva a buscar el vuelo.";
                return RedirectToAction("Buscar", "Vuelos");
            }

            var lista = model.Pasajeros ?? new List<PasajeroFormViewModel>();
            if (lista.Count != operacion.CantidadPasajeros)
            {
                ModelState.AddModelError("", "Complete los datos de todos los pasajeros.");
            }

            for (int i = 0; i < lista.Count; i++)
            {
                var p = lista[i];
                string campo = "Pasajeros[" + i + "].NumeroDocumento";
                if (p.NumeroDocumento != null)
                {
                    p.NumeroDocumento = p.NumeroDocumento.Trim().ToUpper();
                }

                if (p.TipoDocumentoCodigo == "CEDULA" && !ReglasReserva.ValidarCedula(p.NumeroDocumento))
                {
                    ModelState.AddModelError(campo, "La cédula debe tener 10 dígitos y ser válida.");
                }
                if (p.FechaNacimiento != null && (p.FechaNacimiento.Value.Year <= 1900 || p.FechaNacimiento.Value.Date > DateTime.Today))
                {
                    ModelState.AddModelError("Pasajeros[" + i + "].FechaNacimiento", "La fecha de nacimiento no es válida.");
                }
                if (lista.Take(i).Any(x => x.TipoDocumentoCodigo == p.TipoDocumentoCodigo && x.NumeroDocumento == p.NumeroDocumento))
                {
                    ModelState.AddModelError(campo, "Este documento ya está registrado en otro pasajero de la reserva.");
                }

                // Un pasajero no puede tener dos asientos vigentes en el mismo vuelo (UX_RPV_PasajeroActivo)
                var existente = db.Pasajero.FirstOrDefault(x => x.TipoDocumentoCodigo == p.TipoDocumentoCodigo && x.NumeroDocumento == p.NumeroDocumento);
                if (existente != null)
                {
                    var estados = new[] { "PENDIENTE", "CONFIRMADA" };
                    int pasajeroId = existente.PasajeroId;
                    var vuelosOperacion = operacion.VueloIds;
                    if (db.ReservaPasajeroVuelo.Any(a => a.PasajeroId == pasajeroId && vuelosOperacion.Contains(a.VueloId) && estados.Contains(a.EstadoAsignacionCodigo)))
                    {
                        ModelState.AddModelError(campo, "Este pasajero ya tiene un asiento en este vuelo.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var vista = CrearModeloPasajeros(operacion);
                vista.Pasajeros = lista;
                ViewBag.TiposDocumento = db.TipoDocumentoIdentidad.ToList();
                return View(vista);
            }

            operacion.Pasajeros = lista;
            Session["Operacion"] = operacion;
            return RedirectToAction("Confirmar");
        }

        // GET: Reservas/Confirmar
        [Authorize]
        public ActionResult Confirmar()
        {
            var operacion = Session["Operacion"] as OperacionReserva;
            if (operacion == null || !operacion.AsientosCompletos || operacion.Pasajeros == null)
            {
                TempData["Mensaje"] = "No hay una operación en curso. Busque un vuelo.";
                return RedirectToAction("Buscar", "Vuelos");
            }
            return View(CrearModeloConfirmacion(operacion));
        }

        // POST: Reservas/Confirmar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Confirmar(ConfirmarReservaViewModel model, string accion)
        {
            var operacion = Session["Operacion"] as OperacionReserva;
            if (operacion == null || !operacion.AsientosCompletos || operacion.Pasajeros == null)
            {
                TempData["Mensaje"] = "No hay una operación en curso. Busque un vuelo.";
                return RedirectToAction("Buscar", "Vuelos");
            }
            if (!model.AceptaCondiciones)
            {
                ModelState.AddModelError("AceptaCondiciones", "Debe aceptar las condiciones de la reserva.");
                return View(CrearModeloConfirmacion(operacion));
            }

            ReglasReserva.ProcesarExpiraciones(db);
            var listaVuelos = CargarVuelos(operacion.VueloIds);

            // Revalidación de la disponibilidad al confirmar (RNP-04)
            string error = ValidarVuelos(listaVuelos, operacion.CantidadPasajeros);
            var ocupadosAhora = new List<string>();
            foreach (var vuelo in listaVuelos)
            {
                var asientos = ReglasReserva.EstadoAsientos(db, vuelo);
                foreach (var eleccion in operacion.Asientos.Where(a => a.VueloId == vuelo.VueloId))
                {
                    if (!asientos.Any(a => a.NumeroAsiento == eleccion.NumeroAsiento && a.Estado == "DISPONIBLE"))
                    {
                        ocupadosAhora.Add(eleccion.Codigo);
                    }
                }
            }
            if (error == null && ocupadosAhora.Count > 0)
            {
                error = "El asiento " + string.Join(", ", ocupadosAhora.Distinct()) + " ya no está disponible. Seleccione otro.";
            }
            if (error != null)
            {
                TempData["Mensaje"] = error;
                return RedirectToAction("SeleccionAsiento", new { vuelos = operacion.VuelosTexto, pasajeros = operacion.CantidadPasajeros });
            }

            // DP-07: la reserva se retiene 30 minutos, sin pasar de la salida del primer tramo
            var ahora = DateTime.Now;
            var expiracion = ahora.AddMinutes(ReglasReserva.MinutosRetencion);
            var primeraSalida = ReglasReserva.Salida(listaVuelos[0]);
            if (primeraSalida < expiracion)
            {
                expiracion = primeraSalida;
            }
            if (expiracion <= ahora)
            {
                TempData["Mensaje"] = "El vuelo ya no admite reservas.";
                return RedirectToAction("Buscar", "Vuelos");
            }

            Reserva reserva;
            using (var transaccion = db.Database.BeginTransaction())
            {
                try
                {
                    reserva = new Reserva
                    {
                        UsuarioId = User.Identity.GetUserId(),
                        FechaCreacion = ahora,
                        FechaExpiracion = expiracion,
                        EstadoReservaCodigo = "PENDIENTE"
                    };
                    db.Reserva.Add(reserva);

                    for (int i = 0; i < operacion.Pasajeros.Count; i++)
                    {
                        var datos = operacion.Pasajeros[i];
                        var pasajero = db.Pasajero.FirstOrDefault(x => x.TipoDocumentoCodigo == datos.TipoDocumentoCodigo && x.NumeroDocumento == datos.NumeroDocumento);
                        if (pasajero == null)
                        {
                            pasajero = new Pasajero
                            {
                                TipoDocumentoCodigo = datos.TipoDocumentoCodigo,
                                NumeroDocumento = datos.NumeroDocumento,
                                Nombres = datos.Nombres.Trim(),
                                Apellidos = datos.Apellidos.Trim(),
                                FechaNacimiento = datos.FechaNacimiento,
                                Correo = string.IsNullOrWhiteSpace(datos.Correo) ? null : datos.Correo.Trim(),
                                Telefono = string.IsNullOrWhiteSpace(datos.Telefono) ? null : datos.Telefono.Trim(),
                                FechaRegistro = ahora
                            };
                            db.Pasajero.Add(pasajero);
                        }

                        foreach (var vuelo in listaVuelos)
                        {
                            var eleccion = operacion.Asientos.First(a => a.Pasajero == i && a.VueloId == vuelo.VueloId);
                            db.ReservaPasajeroVuelo.Add(new ReservaPasajeroVuelo
                            {
                                Reserva = reserva,
                                Pasajero = pasajero,
                                VueloId = vuelo.VueloId,
                                NumeroAsiento = eleccion.NumeroAsiento,
                                ImporteTramo = vuelo.Frecuencia.Ruta.TarifaReferencial,   // DP-04: tarifa copiada al reservar
                                EstadoAsignacionCodigo = "PENDIENTE",
                                FechaAsignacion = ahora
                            });
                        }
                    }

                    db.SaveChanges();
                    transaccion.Commit();
                }
                catch (Exception)
                {
                    // Si un asiento falla (índice único o trigger de la base), no se guarda nada
                    transaccion.Rollback();
                    TempData["Mensaje"] = "No se pudo registrar la reserva porque uno de los asientos ya no está disponible. Seleccione otro.";
                    return RedirectToAction("SeleccionAsiento", new { vuelos = operacion.VuelosTexto, pasajeros = operacion.CantidadPasajeros });
                }
            }

            Session.Remove("Operacion");
            TempData["Mensaje"] = "Reserva " + reserva.CodigoReserva + " registrada. Estado: Pendiente de pago.";

            if (accion == "pagar")
            {
                return RedirectToAction("Pagar", "Compras", new { id = reserva.ReservaId });
            }
            return RedirectToAction("Confirmacion", "Compras", new { id = reserva.ReservaId });
        }

        // ----- Métodos auxiliares del flujo de reserva -----

        private List<int> LeerVuelos(string vuelos)
        {
            var ids = new List<int>();
            foreach (var parte in (vuelos ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int id;
                if (int.TryParse(parte, out id) && !ids.Contains(id))
                {
                    ids.Add(id);
                }
            }
            return ids;
        }

        // Carga los vuelos en el orden del itinerario
        private List<Vuelo> CargarVuelos(List<int> ids)
        {
            var vuelos = ReglasReserva.VuelosConDetalle(db).Where(v => ids.Contains(v.VueloId)).ToList();
            return ids.Select(id => vuelos.FirstOrDefault(v => v.VueloId == id)).Where(v => v != null).ToList();
        }

        private string ValidarVuelos(List<Vuelo> vuelos, int cantidad)
        {
            string error = ReglasReserva.ValidarSecuencia(vuelos);
            if (error != null)
            {
                return error;
            }
            var ocupados = ReglasReserva.OcupadosPorVuelo(db, vuelos.Select(v => v.VueloId).ToList());
            foreach (var vuelo in vuelos)
            {
                var tramo = ReglasReserva.CrearTramo(vuelo, ocupados);
                string motivo = ReglasReserva.MotivoNoReservable(vuelo, tramo.Disponibles, cantidad);
                if (motivo != null)
                {
                    return tramo.CodigoVuelo + ": " + motivo;
                }
            }
            return null;
        }

        private SeleccionAsientoViewModel CrearModeloSeleccion(List<Vuelo> vuelos, OperacionReserva operacion, string mensaje)
        {
            var ocupados = ReglasReserva.OcupadosPorVuelo(db, vuelos.Select(v => v.VueloId).ToList());
            var model = new SeleccionAsientoViewModel
            {
                CantidadPasajeros = operacion.CantidadPasajeros,
                VuelosIds = operacion.VuelosTexto,
                Seleccionados = operacion.Asientos,
                Mensaje = mensaje ?? TempData["Mensaje"] as string
            };
            foreach (var vuelo in vuelos)
            {
                model.Tramos.Add(new TramoSeleccionViewModel
                {
                    Vuelo = ReglasReserva.CrearTramo(vuelo, ocupados),
                    Asientos = ReglasReserva.EstadoAsientos(db, vuelo)
                });
            }
            return model;
        }

        private PasajerosViewModel CrearModeloPasajeros(OperacionReserva operacion)
        {
            var vuelos = CargarVuelos(operacion.VueloIds);
            var model = new PasajerosViewModel
            {
                Tramos = vuelos.Count,
                Fecha = vuelos[0].FechaOperacion,
                RutaTexto = vuelos[0].Frecuencia.Ruta.Aeropuerto1.Localidad + " → " + vuelos[vuelos.Count - 1].Frecuencia.Ruta.Aeropuerto.Localidad
            };
            for (int p = 0; p < operacion.CantidadPasajeros; p++)
            {
                var codigos = operacion.Asientos.Where(a => a.Pasajero == p)
                    .OrderBy(a => operacion.VueloIds.IndexOf(a.VueloId))
                    .Select(a => a.Codigo);
                model.AsientosPorPasajero.Add(string.Join(", ", codigos));
            }
            return model;
        }

        private ConfirmarReservaViewModel CrearModeloConfirmacion(OperacionReserva operacion)
        {
            var vuelos = CargarVuelos(operacion.VueloIds);
            var ocupados = ReglasReserva.OcupadosPorVuelo(db, vuelos.Select(v => v.VueloId).ToList());
            var model = new ConfirmarReservaViewModel();
            model.Tramos = vuelos.Select(v => ReglasReserva.CrearTramo(v, ocupados)).ToList();

            for (int p = 0; p < operacion.Pasajeros.Count; p++)
            {
                var datos = operacion.Pasajeros[p];
                model.Filas.Add(new FilaPasajeroReserva
                {
                    Nombre = datos.Nombres + " " + datos.Apellidos,
                    Documento = (datos.TipoDocumentoCodigo == "CEDULA" ? "Cédula " : "Pasaporte ") + datos.NumeroDocumento,
                    Asientos = vuelos.Select(v => operacion.Asientos.First(a => a.Pasajero == p && a.VueloId == v.VueloId).Codigo).ToList(),
                    Importe = vuelos.Sum(v => v.Frecuencia.Ruta.TarifaReferencial)
                });
            }
            return model;
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
