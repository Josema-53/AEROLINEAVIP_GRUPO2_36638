using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace AerolineaVIP.Models
{
    // Reglas de negocio compartidas por las pantallas de vuelos, reservas, pagos y check-in.
    // Los valores numéricos son los propuestos en el apartado 18.1 (pendientes de aprobación).
    public static class ReglasReserva
    {
        public const int MinutosConexion = 60;   // DP-10: conexión mínima en Guayaquil
        public const int MinutosRetencion = 30;  // DP-07: retención de una reserva pendiente

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

        public static string NombreDia(int diaSemana)
        {
            string[] dias = { "", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo" };
            return dias[diaSemana];
        }
    }
}
