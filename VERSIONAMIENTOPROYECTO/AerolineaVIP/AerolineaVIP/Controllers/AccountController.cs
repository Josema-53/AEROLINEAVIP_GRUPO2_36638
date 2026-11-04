using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using AerolineaVIP.Models;

namespace AerolineaVIP.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            // Si ya inició sesión y llega aquí, es porque intentó entrar a una sección sin permiso
            if (User.Identity.IsAuthenticated)
            {
                return View("AccesoDenegado");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Se permite ingresar con el nombre de usuario o con el correo
            var nombreUsuario = model.UserName.Trim();
            if (nombreUsuario.Contains("@"))
            {
                var usuarioPorCorreo = await UserManager.FindByEmailAsync(nombreUsuario);
                if (usuarioPorCorreo != null)
                {
                    nombreUsuario = usuarioPorCorreo.UserName;
                }
            }

            var result = await SignInManager.PasswordSignInAsync(nombreUsuario, model.Password, model.RememberMe, shouldLockout: true);
            switch (result)
            {
                case SignInStatus.Success:
                    if (string.IsNullOrEmpty(returnUrl))
                    {
                        var usuario = await UserManager.FindByNameAsync(nombreUsuario);
                        if (await UserManager.IsInRoleAsync(usuario.Id, "SuperUsuario") || await UserManager.IsInRoleAsync(usuario.Id, "Administrador"))
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                        if (await UserManager.IsInRoleAsync(usuario.Id, "Counter"))
                        {
                            return RedirectToAction("Buscar", "Vuelos");
                        }
                        return RedirectToAction("MisReservas", "Reservas");
                    }
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                    return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Código no válido.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            // Un usuario con sesión solo puede registrar cuentas si es SuperUsuario
            if (User.Identity.IsAuthenticated && !User.IsInRole("SuperUsuario"))
            {
                return RedirectToAction("Index", "Home");
            }
            CargarRoles("Cliente");
            return View(new RegisterViewModel { Rol = "Cliente" });
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (User.Identity.IsAuthenticated && !User.IsInRole("SuperUsuario"))
            {
                return RedirectToAction("Index", "Home");
            }

            // El registro público solo permite crear cuentas de cliente
            string rol = "Cliente";
            string[] rolesPermitidos = { "Cliente", "Counter", "Administrador", "SuperUsuario" };
            if (User.IsInRole("SuperUsuario") && rolesPermitidos.Contains(model.Rol))
            {
                rol = model.Rol;
            }

            if (!model.AceptaCondiciones)
            {
                ModelState.AddModelError("AceptaCondiciones", "Debe aceptar el aviso de privacidad y las condiciones.");
            }

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName.Trim(),
                    Email = model.Email.Trim(),
                    Nombres = model.Nombres.Trim(),
                    Apellidos = model.Apellidos.Trim()
                };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var asignacion = await UserManager.AddToRoleAsync(user.Id, rol);
                    if (!asignacion.Succeeded)
                    {
                        await UserManager.DeleteAsync(user);
                        AddErrors(asignacion);
                        CargarRoles(rol);
                        return View(model);
                    }

                    if (User.IsInRole("SuperUsuario"))
                    {
                        // Conserva la sesión del administrador al crear otra cuenta
                        TempData["Mensaje"] = "Cuenta " + user.UserName + " creada correctamente con el rol " + rol + ".";
                        return RedirectToAction("Register");
                    }

                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    return RedirectToAction("Index", "Home");
                }
                AddErrors(result);
            }

            CargarRoles(rol);
            return View(model);
        }

        private void CargarRoles(string seleccionado)
        {
            if (User.IsInRole("SuperUsuario"))
            {
                ViewBag.Rol = new SelectList(new[] { "Cliente", "Counter", "Administrador", "SuperUsuario" }, seleccionado);
            }
            else
            {
                ViewBag.Rol = new SelectList(new[] { "Cliente" }, "Cliente");
            }
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Limita las solicitudes de recuperación dentro de la misma sesión
            if (Session["UltimaRecuperacion"] != null && (DateTime.Now - (DateTime)Session["UltimaRecuperacion"]).TotalSeconds < 60)
            {
                ModelState.AddModelError("", "Ya se registró una solicitud. Espere un minuto antes de intentar nuevamente.");
                return View(model);
            }
            Session["UltimaRecuperacion"] = DateTime.Now;

            string dato = model.Email.Trim();
            var user = dato.Contains("@") ? await UserManager.FindByEmailAsync(dato) : await UserManager.FindByNameAsync(dato);
            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                    string enlace = Url.Action("ResetPassword", "Account",
                        new { code = code, email = user.Email }, Request.Url.Scheme);
                    string mensaje = "Para restablecer su contraseña, abra este enlace: <a href=\""
                        + HttpUtility.HtmlAttributeEncode(enlace) + "\">Restablecer contraseña</a>.";
                    await UserManager.SendEmailAsync(user.Id, "Recuperación de contraseña - Aerolínea VIP", mensaje);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "No se pudo completar el envío. Consulte con el administrador del sistema.");
                    return View(model);
                }
            }

            ViewBag.Mensaje = "Si la cuenta está registrada y tiene correo configurado, recibirá las instrucciones para recuperar el acceso.";
            ModelState.Clear();
            return View(new ForgotPasswordViewModel());
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code, string email)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return View("Error");
            }
            return View(new ResetPasswordViewModel { Code = code, Email = email });
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByEmailAsync(model.Email.Trim());
            if (user == null)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddToRoleAsync(user.Id, "Cliente");
                    if (result.Succeeded)
                    {
                        result = await UserManager.AddLoginAsync(user.Id, info.Login);
                        if (result.Succeeded)
                        {
                            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                            return RedirectToLocal(returnUrl);
                        }
                    }
                    await UserManager.DeleteAsync(user);
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Aplicaciones auxiliares
        // Protección del inicio de sesión externo
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}