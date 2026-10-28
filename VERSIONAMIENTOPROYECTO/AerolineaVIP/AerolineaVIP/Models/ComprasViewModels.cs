using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AerolineaVIP.Models
{
    // Pantalla 11: proceso de compra y pago simulado
    public class PagoViewModel
    {
        // Resumen de la compra (se vuelve a cargar desde la base, no se recibe del navegador)
        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public string RutaTexto { get; set; }
        public DateTime FechaViaje { get; set; }
        public string Horario { get; set; }
        public int Pasajeros { get; set; }
        public string Asientos { get; set; }
        public decimal Total { get; set; }
        public string Moneda { get; set; }
        public string EstadoCompra { get; set; }
        public DateTime? Expira { get; set; }

        // Datos del formulario de pago
        [Required(ErrorMessage = "Seleccione el método de pago.")]
        [Display(Name = "Método de pago")]
        public string MetodoPagoCodigo { get; set; }

        [Required(ErrorMessage = "Seleccione la marca de la tarjeta.")]
        [Display(Name = "Marca de la tarjeta")]
        public string MarcaTarjetaCodigo { get; set; }

        [Required(ErrorMessage = "Ingrese el número de tarjeta.")]
        [RegularExpression(@"^\d{12,19}$", ErrorMessage = "El número de tarjeta solo puede contener dígitos.")]
        [Display(Name = "Número de tarjeta")]
        public string NumeroTarjeta { get; set; }

        [Required(ErrorMessage = "Ingrese el nombre del titular.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ ]+$", ErrorMessage = "Solo letras y espacios.")]
        [Display(Name = "Nombre del titular")]
        public string NombreTitular { get; set; }

        [Required(ErrorMessage = "Ingrese la fecha de vencimiento.")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Use el formato MM/AA.")]
        [Display(Name = "Vencimiento")]
        public string Vencimiento { get; set; }

        [Required(ErrorMessage = "Ingrese el código de seguridad.")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "El código de seguridad solo puede contener 3 o 4 dígitos.")]
        [Display(Name = "Código de seguridad")]
        public string CodigoSeguridad { get; set; }
    }

    // Pantalla 12: confirmación de compra o reserva
    public class DetalleConfirmacion
    {
        public int Tramo { get; set; }
        public string Vuelo { get; set; }
        public string Ruta { get; set; }
        public DateTime Fecha { get; set; }
        public string Horario { get; set; }
        public string Pasajero { get; set; }
        public string Asiento { get; set; }
    }

    public class ConfirmacionViewModel
    {
        public ConfirmacionViewModel()
        {
            Detalle = new List<DetalleConfirmacion>();
        }

        public int ReservaId { get; set; }
        public string CodigoReserva { get; set; }
        public string EstadoReserva { get; set; }
        public string EstadoCompra { get; set; }
        public string EstadoPago { get; set; }
        public string CodigoOperacion { get; set; }
        public string Mensaje { get; set; }
        public string TipoMensaje { get; set; }   // success, info o danger
        public decimal Total { get; set; }
        public string Metodo { get; set; }
        public DateTime? Expira { get; set; }
        public bool PuedePagar { get; set; }
        public bool CompraPagada { get; set; }
        public List<DetalleConfirmacion> Detalle { get; set; }
    }
}
