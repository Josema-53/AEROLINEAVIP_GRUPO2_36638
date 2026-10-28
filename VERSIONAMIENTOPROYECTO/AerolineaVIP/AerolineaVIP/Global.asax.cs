using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using AerolineaVIP;
using AerolineaVIP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace AerolineaVIP
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            var db = new ApplicationDbContext();
            CrearRoles(db);
            CrearSuperUsuario(db);
            CrearAdministrador(db);
            CrearCounter(db);
            CrearClientes(db);
            AsignarPermisos(db);
            db.Dispose();

        }

        private void CrearRoles(ApplicationDbContext db)
        {
            var rolemanager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));

            if (!rolemanager.RoleExists("SuperUsuario"))
            {
                rolemanager.Create(new IdentityRole("SuperUsuario"));
            }
            if (!rolemanager.RoleExists("Administrador"))
            {
                rolemanager.Create(new IdentityRole("Administrador"));
            }
            if (!rolemanager.RoleExists("Counter"))
            {
                rolemanager.Create(new IdentityRole("Counter"));
            }
            if (!rolemanager.RoleExists("Cliente"))
            {
                rolemanager.Create(new IdentityRole("Cliente"));
            }

        }

        private void CrearSuperUsuario(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName("superusuario");
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "superusuario",
                    Email = "superusuario@aerolineavip.ec",
                    Nombres = "Jose Maria",
                    Apellidos = "Sandoval Espinosa"
                };
                var resultado = usermanager.Create(user, "SuperUsuario123");
                if (!resultado.Succeeded)
                {
                    throw new Exception("No se pudo crear el usuario " + user.UserName + ": " + string.Join(" | ", resultado.Errors));
                }
            }
        }

        private void CrearAdministrador(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName("admin01");
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "admin01",
                    Email = "admin@aerolineavip.ec",
                    Nombres = "Diego Alejandro",
                    Apellidos = "Alcaciega Tigse"
                };
                var resultado = usermanager.Create(user, "Admin123");
                if (!resultado.Succeeded)
                {
                    throw new Exception("No se pudo crear el usuario " + user.UserName + ": " + string.Join(" | ", resultado.Errors));
                }
            }
        }

        private void CrearCounter(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName("counter01");
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "counter01",
                    Email = "counter@aerolineavip.ec",
                    Nombres = "Dylan Fernando",
                    Apellidos = "Cajas Rueda"
                };
                var resultado = usermanager.Create(user, "Counter123");
                if (!resultado.Succeeded)
                {
                    throw new Exception("No se pudo crear el usuario " + user.UserName + ": " + string.Join(" | ", resultado.Errors));
                }
            }
        }

        // Los clientes usan el mismo Id que las reservas de ejemplo del script (Reserva.UsuarioId)
        private void CrearClientes(ApplicationDbContext db)
        {
            CrearCliente(db, "7c1e4a20-0001-4b6e-9a01-000000000001", "cliente01", "cliente01@gmail.com", "Mariana Isabel", "Cevallos Ortiz");
            CrearCliente(db, "7c1e4a20-0002-4b6e-9a01-000000000002", "cliente02", "cliente02@yahoo.com", "Carlos Eduardo", "Andrade Villacís");
            CrearCliente(db, "7c1e4a20-0003-4b6e-9a01-000000000003", "cliente03", "cliente03@hotmail.com", "Diego Sebastián", "Zambrano Loor");
            CrearCliente(db, "7c1e4a20-0004-4b6e-9a01-000000000004", "cliente04", "cliente04@gmail.com", "Gabriela Fernanda", "Herrera Castillo");
            CrearCliente(db, "7c1e4a20-0005-4b6e-9a01-000000000005", "cliente05", "cliente05@yahoo.com", "Sebastián Andrés", "Guerrero León");
        }

        private void CrearCliente(ApplicationDbContext db, string id, string nombreUsuario, string correo, string nombres, string apellidos)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            var user = usermanager.FindByName(nombreUsuario);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Id = id,
                    UserName = nombreUsuario,
                    Email = correo,
                    Nombres = nombres,
                    Apellidos = apellidos
                };
                var resultado = usermanager.Create(user, "Cliente123");
                if (!resultado.Succeeded)
                {
                    throw new Exception("No se pudo crear el usuario " + user.UserName + ": " + string.Join(" | ", resultado.Errors));
                }
            }
        }

        private void AsignarPermisos(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

            var SuperUsuario = usermanager.FindByName("superusuario");
            var Administrador = usermanager.FindByName("admin01");
            var Counter = usermanager.FindByName("counter01");

            if (SuperUsuario != null)
            {
                if (!usermanager.IsInRole(SuperUsuario.Id, "SuperUsuario"))
                {
                    usermanager.AddToRole(SuperUsuario.Id, "SuperUsuario");
                }
            }

            if (Administrador != null)
            {
                if (!usermanager.IsInRole(Administrador.Id, "Administrador"))
                {
                    usermanager.AddToRole(Administrador.Id, "Administrador");
                }
            }

            if (Counter != null)
            {
                if (!usermanager.IsInRole(Counter.Id, "Counter"))
                {
                    usermanager.AddToRole(Counter.Id, "Counter");
                }
            }

            string[] clientes = { "cliente01", "cliente02", "cliente03", "cliente04", "cliente05" };
            foreach (var nombreUsuario in clientes)
            {
                var Cliente = usermanager.FindByName(nombreUsuario);
                if (Cliente != null)
                {
                    if (!usermanager.IsInRole(Cliente.Id, "Cliente"))
                    {
                        usermanager.AddToRole(Cliente.Id, "Cliente");
                    }
                }
            }

        }
    }
}
