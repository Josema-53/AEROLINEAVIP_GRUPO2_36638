using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using Microsoft.AspNet.Identity;

namespace AerolineaVIP.Models
{
    // Reglas de negocio compartidas por las pantallas de vuelos, reservas, pagos y check-in.
    // Los valores numéricos son los propuestos en el apartado 18.1 (pendientes de aprobación).
    public static class ReglasReserva
    {
        public const int MinutosConexion = 60;   // DP-10: conexión mínima en Guayaquil
        public const int MinutosRetencion = 30;  // DP-07: retención de una reserva pendiente
        public const int HorasCancelacionPagada = 24;  // DP-08: cancelación de reservas pagadas

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
        // En el modelo: Ruta.Aeropuerto1 = aeropuerto de ORIGEN y Ruta.Aeropuerto = aeropuerto de DESTINO.
        public static IQueryable<Vuelo> VuelosConDetalle(AerolineaVIPContext db)
        {
            return db.Vuelo
                .Include(v => v.Frecuencia.Ruta.Aeropuerto)
                .Include(v => v.Frecuencia.Ruta.Aeropuerto1)
                .Include(v => v.Aeronave.ModeloAeronave);
        }

        // DP-07: las reservas pendientes vencidas pasan a EXPIRADA y liberan sus asientos.
        // Se ejecuta antes de consultar el inventario, confirmar una reserva o pagar.
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

        // Asientos que ocupan inventario por vuelo: asignaciones PENDIENTE (morado) y CONFIRMADA (rojo).
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

        // DP-09: los tramos dominicales Quito–Guayaquil y Guayaquil–Quito solo se venden dentro
        // del itinerario a Galápagos; los tramos Guayaquil–Galápagos sí se venden por separado.
        public static bool VentaIndividualPermitida(Vuelo vuelo)
        {
            if (vuelo.Frecuencia.DiaSemana != 7)
            {
                return true;
            }
            var ruta = vuelo.Frecuencia.Ruta;
            return ruta.Aeropuerto1.CodigoIATA == "GPS" || ruta.Aeropuerto.CodigoIATA == "GPS";
        }

        // Motivo por el que un vuelo no puede reservarse; null si puede reservarse.
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
            if (vuelo.Aeronave == null)
            {
                return "Este vuelo aún no tiene aeronave asignada; sus asientos no pueden reservarse.";
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

        // Estado de los 7 asientos de un vuelo (Verde = disponible, Morado = reservado, Rojo = ocupado).
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

        // Validación de la cédula ecuatoriana (mismo algoritmo que validarCedulaJS en JavaScript)
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

        // Valida que una lista de vuelos forme un viaje válido: un vuelo que se vende solo (DP-09)
        // o tramos contiguos con la conexión mínima (DP-10).
        public static string ValidarSecuencia(List<Vuelo> vuelos)
        {
            if (vuelos.Count == 0)
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

        // Solo el titular de la reserva o personal autorizado (Counter, Administrador, SuperUsuario)
        public static bool PuedeOperar(Reserva reserva, IPrincipal usuario)
        {
            return reserva.UsuarioId == usuario.Identity.GetUserId()
                || usuario.IsInRole("Counter") || usuario.IsInRole("Administrador") || usuario.IsInRole("SuperUsuario");
        }

        // Siguiente número de comprobante académico de un tipo (ACAD-000001, CNF-000001, NC-ACAD-000001)
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

        // Detalle de una reserva: un renglón por tramo, pasajero y asiento
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

        // Salida del primer tramo de una reserva
        public static DateTime? PrimeraSalida(Reserva reserva)
        {
            var vuelos = reserva.ReservaPasajeroVuelo.Select(a => a.Vuelo).Distinct().ToList();
            if (vuelos.Count == 0)
            {
                return null;
            }
            return vuelos.Min(v => Salida(v));
        }

        // DP-08: motivo por el que una reserva no puede cancelarse; null si puede cancelarse
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
