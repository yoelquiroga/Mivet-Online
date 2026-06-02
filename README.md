<div align="center">
  <img src="VeterinariaWebApp/wwwroot/img/icons/logodashboard.png" alt="MiVet Online" width="100">
</div>

# 🐾 MiVet Online

## 📋 Descripción

Sistema de gestión veterinaria online desarrollado para clínicas que aún operan con papel y llamadas, donde las dobles reservas y la falta de trazabilidad clínica eran el problema del día a día. El reto era garantizar reservas atómicas sin colisiones entre usuarios simultáneos, integridad pago-cita y un backend no expuesto al navegador. Se implementó una arquitectura de proxy seguro (MVC → API → DB) con transacciones serializables y bloqueos pesimistas en SQL Server, unique filtered indexes contra doble reserva, hold temporal de 4 min, rate limiting y autenticación por roles con BCrypt. El resultado: cero colisiones de agenda, trazabilidad clínica completa por atención, backend invisible para el cliente final y paneles adaptados a cada rol sobre .NET 8.

## ✨ Funcionalidades

- **Agendamiento atómico** con hold temporal de 4 min, bloqueos pesimistas (UPDLOCK, ROWLOCK) y unique filtered indexes que eliminan la doble reserva en tiempo real
- **Autenticación por roles** (Cliente, Veterinario, Recepcionista) con sesiones seguras (SecurePolicy=Always, SameSite=Strict), BCrypt con migración automática y rate limiting por endpoint
- **Notificaciones SMTP transaccionales** — envío automático de emails ante inasistencias del cliente (con datos de cita y políticas de reembolso) y gestión de respuestas del formulario de contacto público
- **Múltiples métodos de pago** (Yape, Plin, Efectivo, Tarjeta) con verificación de pertenencia y estado de autorización
- **Historial médico completo** por cita — sintomas, diagnóstico, tratamiento, medicamentos, observaciones — con actualización idempotente (INSERT/UPDATE)
- **Paneles dinámicos** con FullCalendar, Chart.js y dashboards adaptados a cada rol, más generación de PDFs con Rotativa

## 🛠 Stack Tecnológico

| Capa | Tecnología |
|---|---|
| **Backend (API)** | ASP.NET Core 8 Web API, C# |
| **Frontend (MVC)** | ASP.NET Core 8 MVC, Razor Views |
| **Base de Datos** | SQL Server + ADO.NET + Stored Procedures |
| **ORM / Data** | ADO.NET con DAO Pattern (sin Entity Framework) |
| **Frontend Assets** | Bootstrap 4, SB Admin 2, JavaScript |
| **Librerías JS** | FullCalendar 5, Chart.js 2, SweetAlert2, Flatpickr |
| **Seguridad** | BCrypt.Net-Next, DotNetEnv, ApiKey Middleware |
| **PDF** | Rotativa (wkhtmltopdf) |
| **Notificaciones** | SMTP (Gmail) vía System.Net.Mail |
| **Auth** | Session-based + API Key + Rate Limiting |

## 📁 Estructura del Proyecto

```
MivetOnline_Proyecto/
├── VeterinariaWebApp/              # Capa MVC (Proxy)
│   ├── Controllers/                # 8 controladores que orquestan llamadas a la API
│   ├── Views/                      # Razor views organizadas por rol
│   ├── Models/                     # ViewModels y DTOs
│   ├── wwwroot/                    # Estáticos (CSS, JS, vendor libs)
│   ├── Rotativa/                   # Binarios wkhtmltopdf
│   └── Program.cs                  # DI, sesión, HttpClient factory
├── VeterinariaAPI/                 # API REST (Gateway)
│   ├── Controllers/                # 8 endpoints REST
│   ├── Repository/
│   │   ├── DAO/                    # 11 DAOs con ADO.NET + SPs
│   │   └── Interfaces/             # 12 contratos de acceso a datos
│   ├── Middleware/                  # ApiKeyMiddleware (validación X-API-Key)
│   ├── Services/                   # AutoCancelCitasService (BackgroundService)
│   ├── Models/                     # Modelos de dominio y DTOs
│   ├── DbConfig.cs                 # Singleton de configuración
│   └── Program.cs                  # CORS, Rate Limiting, compresión, caching
├── DSWEB_MivetOnline_BD.txt        # Schema completo + índices
├── DSWEB_MivetOnline_BD_SP.txt     # Stored procedures (15+ SPs)
├── DSWEB_MivetOnline_Script.txt    # Datos de prueba
├── secure.env                      # Variables de entorno (API Key, DB, SMTP)
└── README.md
```

## 🏗 Arquitectura

```
Navegador ←→ WebApp MVC (Proxy) ←→ API REST (Gateway) ←→ SQL Server
```

**Proxy Pattern**: La WebApp nunca expone la API al cliente. Toda petición viaja del navegador al controlador MVC, que inyecta la API Key vía `HttpClientFactory` y reenvía al backend. Esto elimina vectores IDOR y oculta la superficie de ataque.

**Capa de Seguridad**:
- **ApiKeyMiddleware**: Valida `X-API-Key` en cada request (excepto Swagger)
- **Rate Limiting**: Ventana fija — 300 req/min global, 10 req/min login
- **BCrypt**: Passwords hasheadas con factor de trabajo 11; migración automática desde texto plano
- **Cookie hardening**: HttpOnly, SecurePolicy=Always, SameSite=Strict

**Capa de Datos**:
- **DAO Pattern**: Abstracción completa sobre ADO.NET con stored procedures parametrizados
- **Transacciones serializables** (`IsolationLevel.Serializable`) con `UPDLOCK, ROWLOCK, HOLDLOCK` para evitar condiciones de carrera en reservas simultáneas
- **Unique filtered indexes** sobre `cita(cal_cit, con_cit)` y `cita(cal_cit, ide_vet)` donde `est_cit <> 'C'` — garantía de integridad a nivel BD contra doble reserva
- **Máquina de estados**: `Pendiente(P) → En Atención(E) → Atendida(A)` y `Pendiente(P) → Cancelada(C)` con validación de transiciones en código

**Procesos Automatizados**:
- `AutoCancelCitasService` (BackgroundService): cada 10 min cancela citas Pendientes con más de 30 min de vencidas
- `LimpiarHoldsExpirados`: ejecutado al consultar slots semanales

**Notificaciones**:
- SMTP centralizado en API (Gmail) para emails transaccionales de inasistencia
- Gestión de contacto público con flujo Nuevo → Leído → Respondido + envío de respuesta por email

## 🚀 Requisitos Previos

- .NET SDK 8.0
- SQL Server (Express o superior)
- Visual Studio 2022 / JetBrains Rider / VS Code

## 🔧 Configuración y Ejecución

```bash
# 1. Clonar el repositorio
git clone <repo-url>
cd MivetOnline_Proyecto

# 2. Restaurar paquetes NuGet
dotnet restore VeterinariaAPI/VeterinariaAPI.csproj
dotnet restore VeterinariaWebApp/VeterinariaWebApp.csproj

# 3. Configurar base de datos
# Ejecutar en SQL Server en este orden:
#   1. DSWEB_MivetOnline_BD.txt          (crear BD + tablas + índices)
#   2. DSWEB_MivetOnline_BD_SP.txt       (stored procedures)
#   3. DSWEB_MivetOnline_Script.txt      (opcional — datos de prueba)

# 4. Configurar variables de entorno
# Crear archivo secure.env en la raíz del proyecto:
#   ApiSettings__ApiKey=tu-api-key-segura
#   ConnectionStrings__cn=Server=localhost;Database=BD_MiVetOnline;Trusted_Connection=True;TrustServerCertificate=True
#   EmailConfig__SenderPassword=tu-contraseña-de-aplicacion-gmail

# 5. Ejecutar la API (puerto 7054)
dotnet run --project VeterinariaAPI

# 6. Ejecutar la WebApp (puerto 7084)
dotnet run --project VeterinariaWebApp
```

> **Nota**: La WebApp y la API deben correr simultáneamente. Los puertos se configuran en `Properties/launchSettings.json` de cada proyecto. Las credenciales de prueba están documentadas en `CREDENCIALES-MI VET ONLINE.txt`(solicitar al autor los txt).
