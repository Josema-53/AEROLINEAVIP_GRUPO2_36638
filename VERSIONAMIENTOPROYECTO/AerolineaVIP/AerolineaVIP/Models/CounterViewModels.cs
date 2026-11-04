using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace AerolineaVIP.Models
{
    public class CounterAtencionViewModel
    {
        public CounterAtencionViewModel() { Asignaciones = new List<CounterAsignacionViewModel>(); }
        public int ReservaId { get; set; }
        public string Codigo { get; set; }
        public string Documento { get; set; }
        public bool Buscado { get; set; }
        public bool Encontrada { get; set; }
        public string Mensaje { get; set; }
        public string EstadoReserva { get; set; }
        public List<CounterAsignacionViewModel> Asignaciones { get; set; }
    }

    public class CounterAsignacionViewModel
    {
        public int AsignacionId { get; set; }
        public string Pasajero { get; set; }
        public string Vuelo { get; set; }
        public string Ruta { get; set; }
        public DateTime Salida { get; set; }
        public string AsientoActual { get; set; }
        public string Estado { get; set; }
        public bool PuedeCambiarAsiento { get; set; }
        public List<SelectListItem> AsientosDisponibles { get; set; }
    }
}
