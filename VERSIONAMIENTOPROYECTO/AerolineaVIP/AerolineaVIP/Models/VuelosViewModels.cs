using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AerolineaVIP.Models
{
    // Pantallas 1 y 5: criterios de búsqueda
    public class BusquedaVuelosViewModel
    {
        public BusquedaVuelosViewModel()
        {
            Pasajeros = 1;
        }

        [Required(ErrorMessage = "Seleccione el origen.")]
        [Display(Name = "Origen")]
        public int? OrigenId { get; set; }

        [Required(ErrorMessage = "Seleccione el destino.")]
        [Display(Name = "Destino")]
        public int? DestinoId { get; set; }

        [Required(ErrorMessage = "Indique la fecha de salida.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de salida")]
        public DateTime? Fecha { get; set; }

        [Required(ErrorMessage = "Indique la cantidad de pasajeros.")]
        [Range(1, 7, ErrorMessage = "La cantidad de pasajeros debe estar entre 1 y 7.")]
        [Display(Name = "Pasajeros")]
        public int Pasajeros { get; set; }

        [Display(Name = "Horario")]
        public string Horario { get; set; }
    }

    // Un vuelo (tramo) con sus datos y disponibilidad
    public class TramoVueloViewModel
    {
        public int VueloId { get; set; }
        public string CodigoVuelo { get; set; }
        public string Origen { get; set; }
        public string CiudadOrigen { get; set; }
        public string Destino { get; set; }
        public string CiudadDestino { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan HoraLlegada { get; set; }
        public string Aeronave { get; set; }
        public int Capacidad { get; set; }
        public int Disponibles { get; set; }
        public string Estado { get; set; }
        public decimal Tarifa { get; set; }
    }

    // Pantalla 6: una opción de viaje (vuelo directo o itinerario con escala)
    public class OpcionVueloViewModel
    {
        public OpcionVueloViewModel()
        {
            Tramos = new List<TramoVueloViewModel>();
        }

        public List<TramoVueloViewModel> Tramos { get; set; }
        public bool Comercializable { get; set; }
        public string Motivo { get; set; }

        public bool EsItinerario { get { return Tramos.Count > 1; } }
        public int Disponibles { get { return Tramos.Min(t => t.Disponibles); } }
        public decimal ImportePorPasajero { get { return Tramos.Sum(t => t.Tarifa); } }
        public string VuelosIds { get { return string.Join(",", Tramos.Select(t => t.VueloId)); } }
    }

    public class ResultadosVuelosViewModel
    {
        public ResultadosVuelosViewModel()
        {
            Opciones = new List<OpcionVueloViewModel>();
        }

        public BusquedaVuelosViewModel Busqueda { get; set; }
        public string OrigenTexto { get; set; }
        public string DestinoTexto { get; set; }
        public List<OpcionVueloViewModel> Opciones { get; set; }
        public string Mensaje { get; set; }
    }

    // Un asiento del mapa de cabina
    public class AsientoViewModel
    {
        public byte NumeroAsiento { get; set; }
        public string Codigo { get; set; }
        public byte Fila { get; set; }
        public string Lado { get; set; }
        public string Ubicacion { get; set; }
        public string Estado { get; set; }   // DISPONIBLE, RESERVADO u OCUPADO

        public string EstadoTexto
        {
            get { return Estado == "DISPONIBLE" ? "Disponible" : (Estado == "RESERVADO" ? "Reservado" : "Ocupado"); }
        }

        public string ClaseCss
        {
            get { return Estado == "DISPONIBLE" ? "asiento-disponible" : (Estado == "RESERVADO" ? "asiento-reservado" : "asiento-ocupado"); }
        }
    }

    // Vista parcial del mapa de asientos (pantallas 7, 8 y 19)
    public class MapaAsientosViewModel
    {
        public MapaAsientosViewModel()
        {
            Asientos = new List<AsientoViewModel>();
        }

        public List<AsientoViewModel> Asientos { get; set; }
        public bool Seleccionable { get; set; }
        public bool MostrarLeyenda { get; set; }
    }

    // Pantalla 7: disponibilidad de asientos de un vuelo
    public class DisponibilidadViewModel
    {
        public DisponibilidadViewModel()
        {
            Asientos = new List<AsientoViewModel>();
        }

        public TramoVueloViewModel Vuelo { get; set; }
        public List<AsientoViewModel> Asientos { get; set; }
        public int Pasajeros { get; set; }
        public string VuelosIds { get; set; }
        public DateTime HoraCalculo { get; set; }
        public string Mensaje { get; set; }
        public bool PuedeContinuar { get; set; }

        public int Disponibles { get { return Asientos.Count(a => a.Estado == "DISPONIBLE"); } }
        public int Reservados { get { return Asientos.Count(a => a.Estado == "RESERVADO"); } }
        public int Ocupados { get { return Asientos.Count(a => a.Estado == "OCUPADO"); } }
    }
}
