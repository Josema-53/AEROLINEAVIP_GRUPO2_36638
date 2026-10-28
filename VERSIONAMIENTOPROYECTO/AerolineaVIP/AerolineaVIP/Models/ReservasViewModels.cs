using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AerolineaVIP.Models
{
    // Operación en curso guardada en Session["Operacion"] entre las pantallas 8, 9 y 10.
    // Nada se registra en la base de datos hasta confirmar la reserva (pantalla 10).
    [Serializable]
    public class OperacionReserva
    {
        public OperacionReserva()
        {
            VueloIds = new List<int>();
            Asientos = new List<SeleccionAsientoItem>();
        }

        public List<int> VueloIds { get; set; }
        public int CantidadPasajeros { get; set; }
        public List<SeleccionAsientoItem> Asientos { get; set; }
        public List<PasajeroFormViewModel> Pasajeros { get; set; }

        public bool AsientosCompletos
        {
            get { return Asientos.Count == VueloIds.Count * CantidadPasajeros; }
        }

        public string VuelosTexto
        {
            get { return string.Join(",", VueloIds); }
        }
    }

    // Asiento elegido para un pasajero (0, 1, 2...) en un vuelo
    [Serializable]
    public class SeleccionAsientoItem
    {
        public int Pasajero { get; set; }
        public int VueloId { get; set; }
        public byte NumeroAsiento { get; set; }
        public string Codigo { get; set; }
    }

    // Pantalla 8
    public class TramoSeleccionViewModel
    {
        public TramoVueloViewModel Vuelo { get; set; }
        public List<AsientoViewModel> Asientos { get; set; }
    }

    public class SeleccionAsientoViewModel
    {
        public SeleccionAsientoViewModel()
        {
            Tramos = new List<TramoSeleccionViewModel>();
            Seleccionados = new List<SeleccionAsientoItem>();
        }

        public List<TramoSeleccionViewModel> Tramos { get; set; }
        public int CantidadPasajeros { get; set; }
        public string VuelosIds { get; set; }
        public List<SeleccionAsientoItem> Seleccionados { get; set; }
        public string Mensaje { get; set; }
    }

    // Pantalla 9: datos de una persona que viaja
    [Serializable]
    public class PasajeroFormViewModel
    {
        [Required(ErrorMessage = "Seleccione el tipo de documento.")]
        [Display(Name = "Tipo de documento")]
        public string TipoDocumentoCodigo { get; set; }

        [Required(ErrorMessage = "Ingrese el número de documento.")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "El documento debe tener entre 5 y 20 caracteres.")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "El documento solo puede contener letras y números.")]
        [Display(Name = "Número de documento")]
        public string NumeroDocumento { get; set; }

        [Required(ErrorMessage = "Ingrese los nombres.")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ ]+$", ErrorMessage = "Solo letras y espacios.")]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingrese los apellidos.")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ ]+$", ErrorMessage = "Solo letras y espacios.")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(256)]
        [Display(Name = "Correo")]
        public string Correo { get; set; }

        [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
        [RegularExpression(@"^[0-9+ ]+$", ErrorMessage = "Solo dígitos, espacios y el signo +.")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }
    }

    public class PasajerosViewModel
    {
        public PasajerosViewModel()
        {
            Pasajeros = new List<PasajeroFormViewModel>();
            AsientosPorPasajero = new List<string>();
        }

        public List<PasajeroFormViewModel> Pasajeros { get; set; }
        public List<string> AsientosPorPasajero { get; set; }
        public string RutaTexto { get; set; }
        public DateTime Fecha { get; set; }
        public int Tramos { get; set; }
    }

    // Pantalla 10
    public class FilaPasajeroReserva
    {
        public string Nombre { get; set; }
        public string Documento { get; set; }
        public List<string> Asientos { get; set; }
        public decimal Importe { get; set; }
    }

    public class ConfirmarReservaViewModel
    {
        public ConfirmarReservaViewModel()
        {
            Tramos = new List<TramoVueloViewModel>();
            Filas = new List<FilaPasajeroReserva>();
        }

        public List<TramoVueloViewModel> Tramos { get; set; }
        public List<FilaPasajeroReserva> Filas { get; set; }
        public decimal Total { get { return Filas.Sum(f => f.Importe); } }

        [Display(Name = "Acepto las condiciones de la reserva")]
        public bool AceptaCondiciones { get; set; }
    }
}
