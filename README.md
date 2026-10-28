# Aerolínea VIP · Primera Entrega · Grupo 02

Sistema web monolítico para la compra, reserva y administración de pasajes de una aerolínea VIP.

**Universidad de las Fuerzas Armadas ESPE** · Desarrollo Web para Integración de Tecnologías (NRC 36638)
**Docente:** Ing. Juan Fernando Galarraga Hurtado
**Integrantes:** Diego Alejandro Alcaciega Tigse · Dylan Fernando Cajas Rueda · José María Sandoval Espinosa

## Tecnologías

- ASP.NET MVC 5 sobre .NET Framework 4.8.1, vistas Razor, Bootstrap 5 y jQuery (validación en cliente y en servidor).
- ASP.NET Identity 2.2 en la base `AerolineaVIP_Identity`.
- Entity Framework 6 (Database First) sobre la base de dominio `AerolineaVIP` (modelo `Models/Model_AerolineasVIP.edmx`).
- SQL Server.

## Estructura del repositorio

```
AerolineaVIP.slnx                 Solución (Visual Studio 2022 o superior)
BaseDatos/
  Script_BD_AerolineaVIP.sql      Crea las bases AerolineaVIP y AerolineaVIP_Identity con sus datos iniciales
  Script_Identity_AlterTable.sql  Agrega Nombres y Apellidos a AspNetUsers
AerolineaVIP/                     Proyecto web
  App_Start/                      Rutas, bundles y configuración de Identity (política de contraseñas)
  Controllers/                    Controladores (públicos, de cliente y de administración)
  Models/                         EDMX, IdentityModels, ViewModels y ReglasReserva.cs (reglas de negocio)
  Views/                          Vistas Razor (Shared/_Layout.cshtml es el layout común)
  Content/Site.css                Estilos propios (identidad visual y mapa de asientos)
  Global.asax.cs                  Creación por código de roles, usuarios y asignación de roles
```

## Cómo ejecutar

1. En SQL Server Management Studio ejecute `BaseDatos/Script_BD_AerolineaVIP.sql` completo. Crea las dos bases.
2. Ejecute `BaseDatos/Script_Identity_AlterTable.sql`.
3. Abra `AerolineaVIP.slnx` en Visual Studio. Clic derecho en la solución → **Restaurar paquetes NuGet**.
4. Si su instancia no es la local por defecto (`.`), ajuste en `AerolineaVIP/Web.config` las cadenas `DefaultConnection` (Identity) y `AerolineaVIPContext` (dominio).
5. Compile y ejecute (F5). Al iniciar, `Global.asax.cs` crea los roles y los usuarios si no existen.

## Usuarios creados por código

| Rol | Usuario | Contraseña |
|---|---|---|
| SuperUsuario | `superusuario` | `SuperUsuario123` |
| Administrador | `admin01` | `Admin123` |
| Counter | `counter01` | `Counter123` |
| Cliente | `cliente01` … `cliente05` | `Cliente123` |

El login acepta nombre de usuario o correo. El registro público siempre crea un **Cliente**. Solo el SuperUsuario puede crear cuentas con otro rol (Administración → Crear cuenta de usuario) o cambiar roles (Administración → Usuarios).

## Correspondencia con los 12 entregables del ZIP

| # | Entregable | Dónde revisarlo |
|---|---|---|
| 1 | Home con diseño propio | `HomeController`, `Views/Home/Index.cshtml`, `Content/Site.css` |
| 2–6 | Superusuario, administrador, roles, usuarios iniciales y asignación de roles por código | `Global.asax.cs` (`CrearRoles`, `CrearSuperUsuario`, `CrearAdministrador`, `CrearCounter`, `CrearClientes`, `AsignarPermisos`) |
| 7 | CRUD con scaffolding | Aeropuertos, Rutas, Frecuencias, Vuelos, Itinerarios, TramoItinerarios, Aeronaves, ModeloAeronaves, Pasajeros, Reservas, MetodoPagos, MarcaTarjetas, DatosEmisores |
| 8 | Login con usuario y contraseña | `AccountController.Login` |
| 9 | Registro con usuario, correo, contraseña y rol (seguro) | `AccountController.Register`: el rol lo decide el servidor |
| 10 | 21 interfaces | Ver la tabla siguiente |
| 11 | Mapa de navegación | `_Layout.cshtml` (Inicio · Vuelos · Mi cuenta · Check-in · Administración), panel `Admin/Index` y pestañas `_TabsVuelos` y `_TabsGestion` |
| 12 | Integración de la base de datos | EDMX `AerolineaVIPContext` e Identity `ApplicationDbContext` |

## Las 21 interfaces

| N.º | Pantalla | URL |
|---|---|---|
| 1 | Inicio | `/` |
| 2 | Inicio de sesión | `/Account/Login` |
| 3 | Registro | `/Account/Register` |
| 4 | Recuperar contraseña | `/Account/ForgotPassword` |
| 5 | Búsqueda de vuelos | `/Vuelos/Buscar` |
| 6 | Resultados | `/Vuelos/Resultados` |
| 7 | Disponibilidad del vuelo | `/Vuelos/Disponibilidad` |
| 8 | Selección de asientos (mapa de cabina) | `/Reservas/SeleccionAsiento` |
| 9 | Datos de pasajeros | `/Reservas/Pasajeros` |
| 10 | Confirmación de reserva | `/Reservas/Confirmar` |
| 11 | Pago simulado | `/Compras/Pagar` |
| 12 | Confirmación de compra | `/Compras/Confirmacion` |
| 13 | Mis reservas | `/Reservas/MisReservas` |
| 14 | Gestionar o cancelar reserva | `/Reservas/Gestionar/{id}` |
| 15 | Check-in | `/CheckIn` |
| 16 | Comprobantes (impresión a PDF desde el navegador) | `/Comprobantes` |
| 17 | Panel de administración (sin dashboard) | `/Admin` |
| 18 | Vuelos, frecuencias, itinerarios, rutas y aeropuertos | `/Vuelos/Index` y sus pestañas |
| 19 | Aeronaves | `/Aeronaves` |
| 20 | Usuarios, pasajeros y reservas | `/Admin/Usuarios`, `/Pasajeros`, `/Reservas` |
| 21 | Consultas y reportes | `/Admin/Reportes` |

## Prueba rápida sugerida

1. **Inicio.** Busque Quito → Galápagos un domingo de noviembre (itinerario con escala en Guayaquil).
2. **Compra.** Inicie sesión como `cliente01`, elija asientos, registre pasajeros, confirme y pague.
   - Tarjeta terminada en `0000` → pago **rechazado**.
   - Cualquier otro número válido → pago **aprobado**.
3. **Seguimiento.** En **Mi cuenta**, gestione o cancele la reserva. Revise los comprobantes e imprímalos.
4. **Counter.** Con `counter01`, haga una reserva a nombre de `cliente01` y realice el check-in.
5. **Administración.** Con `admin01`, revise **Administración**:
   - el vuelo con el mapa de asientos;
   - la generación de vuelos y los conflictos de aeronave;
   - los reportes.

## Políticas y alcance

Las políticas DP-01 a DP-15 (apartado 18.1) están en el PDF de documentación entregado en Drive. Las principales son:

| Política | Regla |
|---|---|
| Retención de la reserva | 30 minutos |
| Conexión mínima entre tramos | 60 minutos |
| Cancelación de una reserva pagada | Hasta 24 h antes de la salida |
| Check-in | Abre 24 h antes y cierra 60 min antes |

Los parámetros configurables están en `appSettings` de `Web.config`.
