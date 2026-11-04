using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using Microsoft.AspNet.Identity;

namespace AerolineaVIP.Models
{
    // Reglas de negocio utilizadas por vuelos, reservas, pagos y check-in.
    public static class ReglasReserva
    {
        public const int MinutosConexion = 60;
        public const int MinutosRetencion = 30;
        public const int HorasCancelacionPagada = 24;

        public static string CodigoVuelo(int vueloId)
        {
            return "VIP" + vueloId.ToString("D5");
        }

        public static DateTime Salida(Vuelo vuelo)
        {
            return vuelo.FechaOperacion.Date + vuelo.Frecuencia.HoraSalida;
        }

        public static DateTime Llegada(Vuelo vuelo)
        {
            return vuelo.FechaOperacion.Date + vuelo.Frecuencia.HoraLlegada;
        }

        // Vuelos con ruta, aeropuertos y aeronave cargados.
        // En el modelo: Ruta.Aeropuerto1 = origen y Ruta.Aeropuerto = destino.
        public static IQueryable<Vuelo> VuelosConDetalle(AerolineaVIPContext db)
        {
            return db.Vuelo
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.Aeronave.ModeloAeronave);
        }

        // Las reservas pendientes vencidas liberan sus asientos.
        public static void ProcesarExpiraciones(AerolineaVIPContext db)
        {
            var ahora = DateTime.Now;
            var vencidas = db.Reserva
                .Where(r => r.EstadoReservaCodigo == "PENDIENTE" && r.FechaExpiracion != null && r.FechaExpiracion <= ahora)
                .ToList();
            if (vencidas.Count == 0)
            {
                return;
            }

            foreach (var reserva in vencidas)
            {
                reserva.EstadoReservaCodigo = "EXPIRADA";
                foreach (var asignacion in reserva.ReservaPasajeroVuelo.Where(a => a.EstadoAsignacionCodigo == "PENDIENTE"))
                {
                    asignacion.EstadoAsignacionCodigo = "LIBERADA";
                }
                foreach (var compra in reserva.Compra.Where(c => c.EstadoCompraCodigo == "PENDIENTE"))
                {
                    compra.EstadoCompraCodigo = "ANULADA";
                }
            }
            db.SaveChanges();
        }

        // Asientos pendientes y confirmados que ocupan inventario por vuelo.
        public static Dictionary<int, int> OcupadosPorVuelo(AerolineaVIPContext db, List<int> vueloIds)
        {
            var estados = new[] { "PENDIENTE", "CONFIRMADA" };
            return db.ReservaPasajeroVuelo
                .Where(a => vueloIds.Contains(a.VueloId) && estados.Contains(a.EstadoAsignacionCodigo))
                .GroupBy(a => a.VueloId)
                .Select(g => new { VueloId = g.Key, Total = g.Count() })
                .ToList()
                .ToDictionary(x => x.VueloId, x => x.Total);
        }

        public static TramoVueloViewModel CrearTramo(Vuelo vuelo, Dictionary<int, int> ocupados)
        {
            int capacidad = vuelo.Aeronave != null ? vuelo.Aeronave.ModeloAeronave.CapacidadAsientos : 0;
            int ocupadosVuelo = ocupados.ContainsKey(vuelo.VueloId) ? ocupados[vuelo.VueloId] : 0;
            var ruta = vuelo.Frecuencia.Ruta;

            return new TramoVueloViewModel
            {
                VueloId = vuelo.VueloId,
                CodigoVuelo = CodigoVuelo(vuelo.VueloId),
                Origen = ruta.Aeropuerto1.CodigoIATA,
                CiudadOrigen = ruta.Aeropuerto1.Localidad,
                Destino = ruta.Aeropuerto.CodigoIATA,
                CiudadDestino = ruta.Aeropuerto.Localidad,
                Fecha = vuelo.FechaOperacion,
                HoraSalida = vuelo.Frecuencia.HoraSalida,
                HoraLlegada = vuelo.Frecuencia.HoraLlegada,
                Aeronave = vuelo.Aeronave != null
                    ? vuelo.Aeronave.Matricula + " · " + vuelo.Aeronave.ModeloAeronave.Fabricante + " " + vuelo.Aeronave.ModeloAeronave.Nombre
                    : null,
                Capacidad = capacidad,
                Disponibles = Math.Max(0, capacidad - ocupadosVuelo),
                Estado = vuelo.EstadoVueloCodigo,
                Tarifa = ruta.TarifaReferencial
            };
        }

        // Determinados tramos dominicales solo se venden dentro de un itinerario.
        public static bool VentaIndividualPermitida(Vuelo vuelo)
        {
            if (vuelo.Frecuencia.DiaSemana != 7)
            {
                return true;
            }
            var ruta = vuelo.Frecuencia.Ruta;
            return ruta.Aeropuerto1.CodigoIATA == "GPS" || ruta.Aeropuerto.CodigoIATA == "GPS";
        }

        // Devuelve el motivo por el que no se puede reservar; null si está permitido.
        public static string MotivoNoReservable(Vuelo vuelo, int disponibles, int pasajeros)
        {
            if (vuelo.EstadoVueloCodigo == "CANCELADO")
            {
                return "Vuelo cancelado.";
            }
            if (vuelo.EstadoVueloCodigo == "COMPLETADO" || Salida(vuelo) <= DateTime.Now)
            {
                return "El vuelo ya salió.";
            }
            if (vuelo.EstadoVueloCodigo != "PROGRAMADO" && vuelo.EstadoVueloCodigo != "CONFIRMADO")
            {
                return "Este vuelo no admite reservas.";
            }
            if (!vuelo.Frecuencia.Activa || !vuelo.Frecuencia.Ruta.Activa)
            {
                return "La ruta no está disponible para reservas.";
            }
            if (vuelo.Aeronave == null)
            {
                return "Este vuelo aún no tiene aeronave asignada; sus asientos no pueden reservarse.";
            }
            if (vuelo.Aeronave.EstadoAeronaveCodigo != "DISPONIBLE")
            {
                return "La aeronave no está habilitada para nuevas reservas.";
            }
            if (pasajeros < 1 || pasajeros > 7)
            {
                return "Seleccione entre 1 y 7 pasajeros.";
            }
            if (disponibles == 0)
            {
                return "No quedan asientos disponibles en este vuelo.";
            }
            if (disponibles < pasajeros)
            {
                return "Solo quedan " + disponibles + " asientos disponibles.";
            }
            return null;
        }

        // Verde: disponible; morado: reservado; rojo: ocupado.
        public static List<AsientoViewModel> EstadoAsientos(AerolineaVIPContext db, Vuelo vuelo)
        {
            if (vuelo.Aeronave == null)
            {
                return new List<AsientoViewModel>();
            }

            int modeloId = vuelo.Aeronave.ModeloAeronaveId;
            int vueloId = vuelo.VueloId;
            var estados = new[] { "PENDIENTE", "CONFIRMADA" };
            var asignaciones = db.ReservaPasajeroVuelo
                .Where(a => a.VueloId == vueloId && estados.Contains(a.EstadoAsignacionCodigo))
                .Select(a => new { a.NumeroAsiento, a.EstadoAsignacionCodigo })
                .ToList();

            return db.Asiento
                .Where(s => s.ModeloAeronaveId == modeloId)
                .OrderBy(s => s.NumeroAsiento)
                .ToList()
                .Select(s =>
                {
                    var asignacion = asignaciones.FirstOrDefault(a => a.NumeroAsiento == s.NumeroAsiento);
                    string estado = "DISPONIBLE";
                    if (asignacion != null)
                    {
                        estado = asignacion.EstadoAsignacionCodigo == "CONFIRMADA" ? "OCUPADO" : "RESERVADO";
                    }
                    return new AsientoViewModel
                    {
                        NumeroAsiento = s.NumeroAsiento,
                        Codigo = s.Codigo,
                        Fila = s.Fila,
                        Lado = s.Lado,
                        Ubicacion = s.Ubicacion,
                        Estado = estado
                    };
                })
                .ToList();
        }

        // Validación de cédula ecuatoriana.
        public static bool ValidarCedula(string cedula)
        {
            if (string.IsNullOrEmpty(cedula) || cedula.Length != 10 || !cedula.All(char.IsDigit))
            {
                return false;
            }

            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                int digit = cedula[i] - '0';
                if (i % 2 == 0)
                {
                    int multiplied = digit * 2;
                    sum += (multiplied > 9) ? multiplied - 9 : multiplied;
                }
                else
                {
                    sum += digit;
                }
            }

            int lastDigit = cedula[9] - '0';
            int modulo = sum % 10;
            return (modulo == 0 && lastDigit == 0) || (10 - modulo == lastDigit);
        }

        // Valida la continuidad de los vuelos y el tiempo mínimo de conexión.
        public static string ValidarSecuencia(List<Vuelo> vuelos)
        {
            if (vuelos == null || vuelos.Count == 0)
            {
                return "No se encontró el vuelo seleccionado.";
            }
            if (vuelos.Count == 1 && !VentaIndividualPermitida(vuelos[0]))
            {
                return "Este tramo se vende únicamente como parte del itinerario a Galápagos.";
            }
            for (int i = 1; i < vuelos.Count; i++)
            {
                var anterior = vuelos[i - 1];
                var siguiente = vuelos[i];
                if (anterior.Frecuencia.Ruta.AeropuertoDestinoId != siguiente.Frecuencia.Ruta.AeropuertoOrigenId
                    || (Salida(siguiente) - Llegada(anterior)).TotalMinutes < MinutosConexion)
                {
                    return "Los tramos seleccionados no forman un itinerario válido.";
                }
            }
            return null;
        }

        public static bool ItinerarioRegistrado(AerolineaVIPContext db, List<int> ids)
        {
            if (ids == null || ids.Count == 0 || ids.Distinct().Count() != ids.Count)
            {
                return false;
            }
            if (ids.Count == 1)
            {
                return true;
            }

            var tramos = db.TramoItinerario
                .Where(t => ids.Contains(t.VueloId) && t.Itinerario.EstadoItinerarioCodigo == "ACTIVO")
                .Select(t => new { t.ItinerarioId, t.VueloId, t.Orden }).ToList();

            foreach (var grupo in tramos.GroupBy(t => t.ItinerarioId))
            {
                var secuencia = grupo.OrderBy(t => t.Orden).ToList();
                if (secuencia.Count != ids.Count || !secuencia.Select(t => t.VueloId).SequenceEqual(ids))
                {
                    continue;
                }
                bool contigua = true;
                for (int i = 1; i < secuencia.Count; i++)
                {
                    if (secuencia[i].Orden != secuencia[i - 1].Orden + 1)
                    {
                        contigua = false;
                        break;
                    }
                }
                if (contigua)
                {
                    return true;
                }
            }
            return false;
        }

        // El titular de la reserva y el personal autorizado pueden gestionarla.
        public static bool PuedeOperar(Reserva reserva, IPrincipal usuario)
        {
            return reserva.UsuarioId == usuario.Identity.GetUserId()
                || usuario.IsInRole("Counter") || usuario.IsInRole("Administrador") || usuario.IsInRole("SuperUsuario");
        }

        public static string SiguienteNumero(AerolineaVIPContext db, string tipo, string prefijo)
        {
            var numeros = db.Comprobante.Where(c => c.TipoComprobanteCodigo == tipo).Select(c => c.NumeroComprobante).ToList();
            int maximo = 0;
            foreach (var numero in numeros)
            {
                int valor;
                if (numero.StartsWith(prefijo) && int.TryParse(numero.Substring(prefijo.Length), out valor) && valor > maximo)
                {
                    maximo = valor;
                }
            }
            return prefijo + (maximo + 1).ToString("D6");
        }

        // Construye el detalle de pasajeros y asientos de una reserva.
        public static List<DetalleConfirmacion> CrearDetalle(AerolineaVIPContext db, Reserva reserva)
        {
            var asignaciones = reserva.ReservaPasajeroVuelo
                .Where(a => a.EstadoAsignacionCodigo == "PENDIENTE" || a.EstadoAsignacionCodigo == "CONFIRMADA"
                            || reserva.EstadoReservaCodigo != "PENDIENTE")
                .ToList();
            var vueloIds = asignaciones.Select(a => a.VueloId).Distinct().ToList();
            var vuelos = VuelosConDetalle(db).Where(v => vueloIds.Contains(v.VueloId)).ToList()
                .OrderBy(v => Salida(v)).ToList();
            var asientos = db.Asiento.ToList();

            var detalle = new List<DetalleConfirmacion>();
            for (int t = 0; t < vuelos.Count; t++)
            {
                var vuelo = vuelos[t];
                foreach (var a in asignaciones.Where(x => x.VueloId == vuelo.VueloId).OrderBy(x => x.NumeroAsiento))
                {
                    var asiento = vuelo.Aeronave == null ? null
                        : asientos.FirstOrDefault(s => s.ModeloAeronaveId == vuelo.Aeronave.ModeloAeronaveId && s.NumeroAsiento == a.NumeroAsiento);
                    detalle.Add(new DetalleConfirmacion
                    {
                        Tramo = t + 1,
                        Vuelo = CodigoVuelo(vuelo.VueloId),
                        Ruta = vuelo.Frecuencia.Ruta.Aeropuerto1.Localidad + " → " + vuelo.Frecuencia.Ruta.Aeropuerto.Localidad,
                        Fecha = vuelo.FechaOperacion,
                        Horario = vuelo.Frecuencia.HoraSalida.ToString("hh\\:mm") + "–" + vuelo.Frecuencia.HoraLlegada.ToString("hh\\:mm"),
                        Pasajero = a.Pasajero.Nombres + " " + a.Pasajero.Apellidos,
                        Asiento = asiento != null ? asiento.Codigo : a.NumeroAsiento.ToString()
                    });
                }
            }
            return detalle;
        }

        public static DateTime? PrimeraSalida(Reserva reserva)
        {
            var vuelos = reserva.ReservaPasajeroVuelo.Select(a => a.Vuelo).Distinct().ToList();
            if (vuelos.Count == 0)
            {
                return null;
            }
            return vuelos.Min(v => Salida(v));
        }

        // Indica si una reserva puede cancelarse según su estado y horario.
        public static string MotivoNoCancelable(Reserva reserva)
        {
            if (reserva.EstadoReservaCodigo == "CANCELADA" || reserva.EstadoReservaCodigo == "EXPIRADA")
            {
                return "La reserva ya está " + reserva.EstadoReservaCodigo.ToLower() + ".";
            }
            var salida = PrimeraSalida(reserva);
            if (salida == null || salida <= DateTime.Now)
            {
                return "El vuelo ya salió; la reserva no puede cancelarse.";
            }
            if (reserva.EstadoReservaCodigo == "CONFIRMADA")
            {
                if (salida.Value.AddHours(-HorasCancelacionPagada) < DateTime.Now)
                {
                    return "Las reservas pagadas solo pueden cancelarse hasta " + HorasCancelacionPagada + " horas antes de la salida.";
                }
                if (reserva.ReservaPasajeroVuelo.Any(a => a.CheckIn.Any(c => c.EstadoCheckInCodigo == "COMPLETADO")))
                {
                    return "La reserva tiene pasajeros con check-in completado; no puede cancelarse.";
                }
            }
            return null;
        }

        public static string NombreDia(int diaSemana)
        {
            string[] dias = { "", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo" };
            return dias[diaSemana];
        }
    }
}