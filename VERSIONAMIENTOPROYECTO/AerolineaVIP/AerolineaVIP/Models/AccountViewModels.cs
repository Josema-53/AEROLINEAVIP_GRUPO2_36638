using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AerolineaVIP.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }
    }

    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }

    public class SendCodeViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        [Display(Name = "Código")]
        public string Code { get; set; }
        public string ReturnUrl { get; set; }

        [Display(Name = "¿Recordar este explorador?")]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ForgotViewModel
    {
        [Required]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }
    }

    public class LoginViewModel
    {

        [Required(ErrorMessage = "Ingrese su nombre de usuario o correo.")]
        [StringLength(256)]
        [Display(Name = "Nombre de usuario o correo")]
        public string UserName { get; set; }

        /*****************
        [Required]
        [Display(Name = "Correo electrónico")]
        [EmailAddress]
        public string Email { get; set; }
        *********/

        [Required(ErrorMessage = "Ingrese su contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Display(Name = "¿Recordar cuenta?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ingrese sus nombres.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Los nombres deben tener entre 2 y 100 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ ]+$", ErrorMessage = "Los nombres solo pueden contener letras y espacios.")]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; }

        [Required(ErrorMessage = "Ingrese sus apellidos.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre 2 y 100 caracteres.")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ ]+$", ErrorMessage = "Los apellidos solo pueden contener letras y espacios.")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "Ingrese un nombre de usuario.")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "El nombre de usuario debe tener entre 4 y 50 caracteres.")]
        [RegularExpression(@"^[a-zA-Z0-9._]+$", ErrorMessage = "Use solo letras, números, punto o guion bajo, sin espacios.")]
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Ingrese su correo electrónico.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(256)]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Ingrese una contraseña.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "La contraseña debe tener al menos una mayúscula, una minúscula y un número.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirme la contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }

        // El servidor decide el rol final: un visitante siempre queda como Cliente
        [Display(Name = "Rol")]
        public string Rol { get; set; }

        [Display(Name = "Acepto el aviso de privacidad y las condiciones")]
        public bool AceptaCondiciones { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "El número de caracteres de {0} debe ser al menos {2}.", MinimumLength = 3)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "La contraseña y la contraseña de confirmación no coinciden.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        // Pantalla 4: se acepta el correo o el nombre de usuario
        [Required(ErrorMessage = "Ingrese su correo o nombre de usuario.")]
        [StringLength(256)]
        [Display(Name = "Correo o nombre de usuario")]
        public string Email { get; set; }
    }
}
