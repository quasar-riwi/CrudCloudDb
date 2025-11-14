# CrudCloud API

[Este README será reemplazado por la versión final en español e inglés solicitada]

# CrudCloud API — Documentación (Español / English)

> Documento bilingüe (español / inglés). Primero la versión en español, seguida por la versión en inglés.

<!-- ==========================
     VERSIÓN EN ESPAÑOL
     ========================== -->

# CrudCloud — API (ES)

## 1. Introducción general

CrudCloud es una API backend para gestionar usuarios, instancias de bases de datos y pagos (integra con Mercado Pago), además de enviar notificaciones vía Discord y emails SMTP. Está pensada para equipos técnicos y no técnicos que desean un servicio SaaS para provisionar bases de datos remotas (MySQL, PostgreSQL, SQL Server, MongoDB) y gestionar suscripciones.

- Lenguaje: C#
- Framework: .NET 8 (ASP.NET Core Web API)
- ORM: Entity Framework Core
- Dependencias relevantes: HttpClient, Microsoft.EntityFrameworkCore, Npgsql (Postgres), MySql.Data (MySQL), MongoDB.Driver, AutoMapper, BCrypt.Net-Next, System.IdentityModel.Tokens.Jwt

Problema que resuelve: Permite a usuarios registrarse, crear/gestionar instancias de bases de datos y pagar por planes mediante Mercado Pago. También ofrece auditoría, salud de instancias, y notificaciones por Discord y correo.

Usuarios objetivo: equipos que ofrecen bases de datos como servicio y desarrolladores que necesiten un backend listo para gestionar usuarios, pagos y provisión de instancias.

---

## 2. Estructura del proyecto

Estructura principal (seleccionada):

- `Controllers/` — Contiene los controladores HTTP que exponen la API:
  - `UsersController.cs` — Endpoints de autenticación, registro, verificación de email, recuperación y gestión de usuarios (/api/users/*).
  - `PaymentsController.cs` — Endpoints para crear suscripciones, pagos únicos y recibir webhooks de Mercado Pago (/api/payments/*).
  - `DatabaseInstancesController.cs` — Endpoints para crear y eliminar instancias de BD del usuario (/api/databaseinstances).
  - `AuditLogsController.cs` — Endpoints para leer logs de auditoría (/api/auditlogs).
  - `HealthController.cs` — Endpoint para verificar el estado de las instancias (conexión a cada motor) (/api/health/instances).

- `Services/` — Lógica de negocio y wrappers de integración:
  - `UserService.cs` — Registro, login, verificación de email, cambio de contraseña, lógica JWT.
  - `MercadoPagoService.cs` — Creación de preferencias en Mercado Pago, creación/actualización de suscripciones, procesamiento de webhooks y persistencia de pagos.
  - `DatabaseInstanceService.cs` — Lógica de provisión/eliminación de instancias (generación de credenciales, llamadas a DatabaseCreator).
  - `EmailService.cs` — Plantillas y envío de correos vía SMTP; envía notificaciones también a Discord vía `DiscordWebhookService`.
  - `DiscordWebhookService.cs` — Envía embeds a distintos webhooks configurados en `appsettings.json`.
  - `AuditService.cs` — Registro de acciones en tabla de auditoría.

- `Data/` — Contexto de EF Core y entidades (migrations en `Migrations/`).
- `DTOs/` — Objetos usados por los controladores para recibir/retornar información (p. ej. `UserRegisterDto`, `CreateSubscriptionRequest`).
- `Mappings/` — Perfil de AutoMapper para mapear entidades a DTOs.
- `Utils/` — Utilidades: `DatabaseCreator` (para crear/eliminar instancias reales), `PasswordHasher`, `TokenGenerator`, `PlanLimits`, etc.
- `Models/` — Modelos de configuración (por ejemplo: `EmailSettings.cs`, `DiscordWebhookSettings.cs`, `MercadoPagoSettings.cs`) y modelos de negocio (`User.cs`).

Integraciones externas detectadas:
- Mercado Pago (API para crear pagos/suscripciones y webhook).
- Discord Webhooks (varios: Auth events, DB instances, Payments, Errors, Email validations).
- SMTP (Gmail en `appsettings.json` de ejemplo).
- Bases de datos: Postgres, MySQL, SQL Server y MongoDB (soporte para crear/monitorizar instancias).

---

## 3. Requisitos previos

- .NET SDK: recomendado .NET 8 (se detecta uso de net8.0 en bin/). Version minima: .NET 7 si no puede usar 8, pero el proyecto está configurado para .NET 8.
- Base de datos para la propia API (EF Core) — ejemplo usa Postgres/SQL Server/MySQL según ConnectionStrings.
- Cuenta y credenciales de Mercado Pago (Access Token y Public Key).
- Webhooks de Discord (URLs de webhooks).
- SMTP para envío de correos (p. ej. Gmail con App Passwords).

Dependencias (sugeridas para restaurar si hace falta):
- dotnet restore (restaurará los paquetes desde el .csproj)

Variables/archivos de configuración:
- `appsettings.json` (ejemplo ya presente en el proyecto). Contiene:
  - `ConnectionStrings` — conexiones a DB usadas por utilidades y EF.
  - `Jwt` — `Key`, `Issuer`, `Audience`, `ExpirationInMinutes`.
  - `EmailSettings` — SMTP server, port, credentials.
  - `DiscordWebhookSettings` — URLs de los webhooks.
  - `MercadoPago` — `AccessToken`, `PublicKey`, `BaseUrl`, `WebhookSecret`, `WebhookBaseUrl`, `FrontendBaseUrl`.

---

## 4. Guía de instalación y ejecución

Checklist de pasos (Windows / cmd.exe):

1. Clona el repositorio:

   git clone <tu-repo-url>
   cd CrudCloudDb

2. Restaurar dependencias y compilar:

   dotnet restore
   dotnet build

3. Configura `appsettings.json` (ver sección 5). Puedes crear `appsettings.Development.json` para valores locales.

4. Migraciones y base de datos (si usas EF Migrations):

   dotnet ef database update

   Nota: Asegúrate de que la cadena de conexión en `appsettings.json` (`ConnectionStrings:PostgresAdmin` u otra) apunte a la base de datos correcta.

5. Ejecutar la API:

   dotnet run --project CrudCloud.api.csproj

   - Por defecto, el API expondrá rutas en `https://localhost:5001` y `http://localhost:5000` (según `launchSettings.json`).

Ejemplo de comandos completos (cmd.exe):

```cmd
cd C:\Users\user\OneDrive\Escritorio\CrudCloudDb
dotnet restore
dotnet build
dotnet run --project CrudCloud.api.csproj
```

Cómo levantar backend y frontend: Este repo contiene sólo el backend API (no he detectado carpeta frontend). Si tienes un frontend separado, configura `MercadoPago:FrontendBaseUrl` en `appsettings.json` para que las rutas de retorno apunten correctamente.

---

## 5. Configuración de credenciales externas

A continuación se explica cómo obtener/usar las claves para las integraciones detectadas. El archivo de configuración principal es `appsettings.json`. Ejemplo parcial (extraído del repo):

```json
{
  "Jwt": {
    "Key": "kaqoD7k5gHcK8p6p6DExIr6zu6Ccm8d3!",
    "Issuer": "https://service.quasar.andrescortes.dev",
    "Audience": "https://service.quasar.andrescortes.dev",
    "ExpirationInMinutes": 60
  },

  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderName": "CrudCloud",
    "SenderEmail": "omaruribe0609@gmail.com",
    "Username": "omaruribe0609@gmail.com",
    "Password": "APP_PASSWORD",
    "EnableSsl": true
  },

  "DiscordWebhookSettings": {
    "AuthEventsWebhookUrl": "URL_WEBHOOK_AUTH",
    "DbInstancesEventsWebhookUrl": "URL_WEBHOOK_DB",
    "PaymentEventsWebhookUrl": "URL_WEBHOOK_PAYMENTS",
    "SystemErrorsWebhookUrl": "URL_WEBHOOK_ERRORS",
    "EmailValidationWebhookUrl": "URL_WEBHOOK_EMAIL"
  },

  "MercadoPago": {
    "AccessToken": "ACCESS_TOKEN",
    "PublicKey": "PUBLIC_KEY",
    "BaseUrl": "https://api.mercadopago.com",
    "UseSandbox": false,
    "WebhookSecret": "WEBHOOK_SECRET",
    "WebhookBaseUrl": "https://your-backend.example.com",
    "FrontendBaseUrl": "https://your-frontend.example.com"
  }
}
```

Cómo obtener cada credencial:

- Discord (Webhooks):
  1. Abre Discord y ve al servidor y canal donde quieres recibir notificaciones.
  2. Ve a Configuración del canal -> Integraciones -> Webhooks -> Crear webhook.
  3. Copia la URL del webhook y pégala en `appsettings.json` en la propiedad correspondiente.

- Mercado Pago:
  1. Crea una cuenta en https://www.mercadopago.com/ (o ingresa a tu cuenta).
  2. Ve a la sección de `Credenciales` en el dashboard.
  3. Copia `ACCESS_TOKEN` y `PUBLIC_KEY` a las propiedades `MercadoPago:AccessToken` y `MercadoPago:PublicKey`.
  4. `WebhookSecret` se usa si deseas validar el contenido del webhook — puedes establecer un secreto y validar la firma manualmente (el código actual no valida firma, pero registra `WebhookSecret` en configuración).
  5. `WebhookBaseUrl` debe apuntar a tu API pública que recibe notificaciones: por ejemplo `https://mi-backend.com` — la ruta completa del webhook es `POST {WebhookBaseUrl}/api/payments/webhook`.

- Gmail / SMTP (contraseña de aplicación):
  1. Habilita 2FA en tu cuenta de Google.
  2. Ve a Seguridad -> Contraseñas de aplicaciones -> Generar contraseña -> seleccionar "Mail" y el dispositivo "Otro".
  3. Copia la contraseña de aplicación generada y pégala en `EmailSettings:Password`.
  4. Asegúrate de usar `SmtpServer: smtp.gmail.com` y `SmtpPort: 587` con `EnableSsl: true`.

- Bases de datos y hosts:
  - `ConnectionStrings` y `Hosts` se usan en el código. Ejemplo detectado en `appsettings.json` del repositorio:

```json
{
  "ConnectionStrings": {
    "PostgresAdmin": "Host=88.198.127.218;Port=5432;Database=CrudCloudDB-Quasar;Username=postgres;Password=PASSWORD",
    "MySQLAdmin": "Server=88.198.127.218;Port=3307;User=root;Password=PASSWORD",
    "SqlServerAdmin": "Server=88.198.127.218,1433;User Id=sa;Password=PASSWORD;Database=master;TrustServerCertificate=True;",
    "MongoAdmin": "mongodb://admin:admin@88.198.127.218:27017/admin"
  }
}
```

Recomendación: No versionar `appsettings.json` con credenciales reales. Usar `appsettings.Development.json` o variables de entorno para almacenar secretos.

---

## 6. Uso del sistema

EndPoints principales (base: `/api`):

1. Usuarios (`/api/users`):
   - POST /api/users/register — Registrar un nuevo usuario
     - Body ejemplo (JSON):
       {
         "nombre": "Omar",
         "apellido": "Uribe",
         "correo": "omar@example.com",
         "contraseña": "secret123",
         "plan": "gratis"
       }
     - Respuesta: 201 Created con userId.

   - POST /api/users/login — Login (devuelve JWT)
     - Body ejemplo:
       {
         "correo": "omar@example.com",
         "contraseña": "secret123"
       }
     - Respuesta: 200 OK con token JWT.

   - GET /api/users — Obtener todos los usuarios (requiere JWT)
   - GET /api/users/{id} — Obtener detalle del usuario
   - PUT /api/users/{id} — Actualizar usuario (si cambia plan, envía notificaciones)
   - PATCH /api/users/{id}/status — Activar/Desactivar usuario
   - GET /api/users/verify-email?token=... — Verificar correo
   - POST /api/users/forgot-password — Solicitar reseteo (envía email)
   - POST /api/users/reset-password — Restablecer password con token
   - POST /api/users/change-password — Cambiar contraseña (requiere JWT)

2. Instancias de BD (`/api/databaseinstances`):
   - GET /api/databaseinstances — Listar instancias del usuario.
   - POST /api/databaseinstances — Crear una nueva instancia (body: DatabaseInstanceCreateDto)
   - DELETE /api/databaseinstances/{id} — Eliminar instancia propia

3. Pagos (`/api/payments`):
   - POST /api/payments/subscribe — Crear preferencia de suscripción (requiere JWT)
     - Body: { "plan": "intermedio" }
     - Respuesta: initPointUrl (redirigir usuario a MP)
   - POST /api/payments/one-time-payment — Crear preferencia de pago único
   - POST /api/payments/webhook — Endpoint público que Mercado Pago llama con notificaciones
   - POST /api/payments/test-webhook — Simular webhook (requiere JWT) — útil para pruebas

4. Auditoría (`/api/auditlogs`):
   - GET /api/auditlogs — Obtener logs
   - GET /api/auditlogs/user/{userId} — Logs filtrados por usuario

5. Health (`/api/health/instances`):
   - GET /api/health/instances — Valida la conexión a las instancias registradas (MySQL, Postgres, SQL Server, MongoDB)

Cómo probar flujo típico (registro → verificar email → login → crear instancia → suscribirse):

1. Registrar: POST /api/users/register
2. Click en enlace de verificación (recibido por email) que apunta a: `https://quasar.andrescortes.dev/verify-email?token={token}` — o llamar: GET /api/users/verify-email?token={token}
3. Login: POST /api/users/login → obtener JWT
4. Crear instancia: POST /api/databaseinstances (Authorization: Bearer {token})
5. Crear suscripción: POST /api/payments/subscribe (Authorization: Bearer {token})
6. Completar pago en Mercado Pago → Mercado Pago llamará a /api/payments/webhook

---

## 7. Estructura del código (archivos clave)

- `UserService.cs` — Responsable de registro, login, verificación de email, tokens JWT y manejo de contraseñas (usa `PasswordHasher` y `TokenGenerator`). Genera JWT con las claves en `Jwt` (Key, Issuer, Audience).

- `EmailService.cs` — Plantillas HTML ricas y envíos SMTP. Métodos importantes:
  - `SendEmailVerificationAsync(email, name, token)` — Envía link de verificación (Frontend URL usada en plantilla).
  - `SendPasswordResetAsync(email, name, token)` — Envía link de reseteo.
  - `SendPaymentConfirmationAsync(...)` — Confirma pago al usuario y notifica via Discord.

- `MercadoPagoService.cs` — Integra con la API de Mercado Pago para crear preferencias (checkout/preferences) y consultar pagos. Maneja:
  - `CreateOneTimePaymentAsync(userId, plan)`
  - `CreateSubscriptionAsync(userId, plan)`
  - `ProcessPaymentNotificationAsync(notification)` — Lógica que extrae payment id y actualiza la BD local.
  - `GetPlanConfiguration(plan)` — Devuelve IDs y precios por plan.

- `DiscordWebhookService.cs` — Envía varios tipos de embeds a URLs configuradas: eventos de auth, bases de datos, pagos, errores y validación de email.

- `DatabaseInstanceService.cs` — Gestiona límites por plan (`PlanLimits`), crea nombres/credenciales y llama a `DatabaseCreator` para provisionar instancias reales.

Flujo visual simplificado (crear instancia):
1. Cliente hace POST /api/databaseinstances con DTO.
2. `DatabaseInstancesController` valida JWT y llama a `DatabaseInstanceService.CreateInstanceAsync`.
3. `DatabaseInstanceService` valida límites, genera credenciales y llama a `DatabaseCreator.CrearInstanciaRealAsync`.
4. Si se crea correctamente, se guarda la entidad en DB, se envía email al usuario y se registra en auditoría.

---

## 8. Resolución de problemas comunes

1. Error: "Fallo al enviar email" o excepciones desde SMTP
   - Verifica `EmailSettings` en `appsettings.json`.
   - Si usas Gmail, asegúrate de tener 2FA activado y usar una contraseña de aplicación.
   - Prueba la conexión SMTP con herramientas externas (telnet smtp.gmail.com 587) y revisa logs.

2. Webhook de Mercado Pago no llama o reintentos constantes
   - Asegúrate de que `WebhookBaseUrl` apunte a una URL pública y que el endpoint `/api/payments/webhook` esté accesible.
   - La API devuelve 200 OK para notificaciones válidas; si devuelve 500 Mercado Pago reintentará. Revisa logs para ver la respuesta y ajustar.

3. Error de credenciales en Mercado Pago
   - Revisa `MercadoPago:AccessToken` en `appsettings.json`.
   - Verifica si estás usando sandbox y ajusta `UseSandbox` y `BaseUrl` correctamente.

4. Problemas creando instancias (DatabaseCreator)
   - `DatabaseCreator` es la pieza que realiza la provisión real; si falla, revisa la configuración de hosts (`Hosts:Postgres`, `Hosts:MySQL`, etc.) y las cadenas de conexión administrativas (`ConnectionStrings`).
   - Revisa límites por plan en `PlanLimits.cs`.

5. JWT inválido o error 401
   - Verifica claves en `Jwt:Key`, `Issuer` y `Audience`.
   - Asegúrate de enviar el header Authorization: `Bearer <token>` en requests protegidos.

Recomendaciones de debugging:
- Activar logging en `appsettings.Development.json` con nivel `Information` o `Debug`.
- Revisar logs del servicio (console o proveedor configurado) y los webhooks enviados a Discord para depuración.

---

## 9. Personalización y despliegue

Cambiar endpoints/puertos:
- Ajustar `launchSettings.json` o pasar variables de entorno: `ASPNETCORE_URLS` para cambiar la URL(s) que escucha la aplicación.
- Cambiar `appsettings.json` para apuntar a otra base de datos o hosts.

Despliegue con Docker:
- El repo ya tiene `Dockerfile` y `docker-compose.yml`. Pasos rápidos:

  - Build:

    docker build -t crudcloud-api .

  - Run (ejemplo):

    docker run -e ASPNETCORE_ENVIRONMENT=Production -p 80:80 -p 443:443 crudcloud-api

  - Con `docker-compose`:

    docker-compose up --build

Despliegue en Azure App Service:
- Publica el artefacto (zip) y configura las variables de entorno (APP SETTINGS) con las keys del `appsettings.json` que contienen secretos.

Consideraciones de seguridad:
- No versionar secretos. Usar Azure Key Vault, AWS Secrets Manager o variables de entorno en Docker/Kubernetes.
- Usar HTTPS en producción y habilitar TrustServerCertificate=False si aplicable.

---

## 10. Licencia y contacto

Licencia: No se encontró un archivo `LICENSE` en el repo. Añade uno (por ejemplo MIT) si deseas compartir.

Contacto / Contribución:
- Maintainer detectado en `appsettings.json`: `omaruribe0609@gmail.com` (email presente en el ejemplo). Úsalo como referencia para soporte interno.


<!-- ==========================
     ENGLISH VERSION
     ========================== -->

# CrudCloud — API (EN)

## 1. General introduction

CrudCloud is a backend API to manage users, database instances and payments (integrates with Mercado Pago), plus notifications via Discord and SMTP email. It's intended for mixed technical and non-technical teams offering database-as-a-service functionality.

- Language: C#
- Framework: .NET 8 (ASP.NET Core Web API)
- ORM: Entity Framework Core
- Key dependencies: HttpClient, Microsoft.EntityFrameworkCore, Npgsql, MySql.Data, MongoDB.Driver, AutoMapper, BCrypt.Net-Next, System.IdentityModel.Tokens.Jwt

Problem solved: allow users to register, provision and manage database instances and pay for plans through Mercado Pago. Includes auditing, instance health checks and notifications.

Target audience: SaaS teams providing database services and developers needing a ready backend for user/payment/instance management.

---

## 2. Project structure

Main folders:

- `Controllers/` — HTTP controllers:
  - `UsersController.cs` — Authentication, registration, email verification, password flows (/api/users/*).
  - `PaymentsController.cs` — Create subscriptions, one-time payments and webhook processing (/api/payments/*).
  - `DatabaseInstancesController.cs` — Create/delete user DB instances (/api/databaseinstances).
  - `AuditLogsController.cs` — Read audit logs (/api/auditlogs).
  - `HealthController.cs` — Validate connections to provisioned instances (/api/health/instances).

- `Services/` — Business logic and integrations:
  - `UserService.cs`, `MercadoPagoService.cs`, `DatabaseInstanceService.cs`, `EmailService.cs`, `DiscordWebhookService.cs`, `AuditService.cs`.

- `Data/`, `DTOs/`, `Mappings/`, `Utils/`, `Models/` — EF Core context, DTOs, mappings, helpers and configuration models.

External integrations:
- Mercado Pago, Discord Webhooks, SMTP (Gmail), remote database engines (Postgres, MySQL, SQL Server, MongoDB).

---

## 3. Prerequisites

- .NET SDK 8 recommended.
- A database for the API (Postgres, SQL Server, or MySQL depending on `ConnectionStrings`).
- Mercado Pago account for tokens.
- Discord webhooks URLs.
- SMTP credentials (Gmail app password recommended).

Restore packages:

  dotnet restore

Configuration: `appsettings.json` or environment variables. Key sections: `ConnectionStrings`, `Jwt`, `EmailSettings`, `DiscordWebhookSettings`, `MercadoPago`.

---

## 4. Installation and run guide

Steps (Windows / cmd.exe):

1. Clone repository

   git clone <your-repo>
   cd CrudCloudDb

2. Restore and build

   dotnet restore
   dotnet build

3. Configure `appsettings.json` (see section 5)

4. Apply EF migrations

   dotnet ef database update

5. Run API

   dotnet run --project CrudCloud.api.csproj

Example (cmd.exe):

```cmd
cd C:\Users\user\OneDrive\Escritorio\CrudCloudDb
dotnet restore
dotnet build
dotnet run --project CrudCloud.api.csproj
```

No frontend detected in this repository. If you have a separate frontend, set `MercadoPago:FrontendBaseUrl` and `AppSettings:FrontendUrl` to your frontend origin.

---

## 5. External credentials configuration

The main keys in `appsettings.json` (redacted) are:

```json
{
  "Jwt": { "Key": "...", "Issuer": "...", "Audience": "...", "ExpirationInMinutes": 60 },
  "EmailSettings": { "SmtpServer": "smtp.gmail.com", "SmtpPort": 587, "SenderName": "CrudCloud", "SenderEmail": "omaruribe0609@gmail.com", "Username": "omaruribe0609@gmail.com", "Password": "APP_PASSWORD", "EnableSsl": true },
  "DiscordWebhookSettings": { "AuthEventsWebhookUrl": "URL", "DbInstancesEventsWebhookUrl": "URL", "PaymentEventsWebhookUrl": "URL", "SystemErrorsWebhookUrl": "URL", "EmailValidationWebhookUrl": "URL" },
  "MercadoPago": { "AccessToken": "ACCESS_TOKEN", "PublicKey": "PUBLIC_KEY", "BaseUrl": "https://api.mercadopago.com", "WebhookBaseUrl": "https://your-backend.example.com", "FrontendBaseUrl": "https://your-frontend.example.com" }
}
```

How to obtain each credential and where to place it:

- Discord (Webhooks):
  1. Open the channel settings in Discord -> Integrations -> Webhooks -> Create webhook.
  2. Copy the webhook URL and paste it into `DiscordWebhookSettings` in `appsettings.json` for the corresponding event.

- Mercado Pago:
  1. Login to Mercado Pago and go to the developer/dashboard credentials section.
  2. Copy `ACCESS_TOKEN` (use in `MercadoPago:AccessToken`) and `PUBLIC_KEY`.
  3. Set `WebhookBaseUrl` to the public URL of your API (the webhook endpoint is POST {WebhookBaseUrl}/api/payments/webhook).
  4. (Optional) Configure sandbox vs production by adjusting `BaseUrl` and `UseSandbox` if needed.

- Gmail / SMTP (App Password):
  1. Enable 2FA on the Google account used for sending.
  2. Create an App Password under Security -> App passwords and pick Mail.
  3. Put the generated password into `EmailSettings:Password` and use `smtp.gmail.com:587` with TLS/STARTTLS.

- Database admin connections and hosts:
  - Fill `ConnectionStrings` with admin/management credentials that `DatabaseCreator` may use to provision real DB instances. In the repository example the admin hosts point to `88.198.127.218`.

Security recommendation: do not store secrets in the repository. Use environment variables, `appsettings.Development.json`, or a secrets manager in production.

---

## 6. Using the system

This section mirrors the Spanish guide and adds concrete examples.

Primary endpoints (base `/api`) and usage notes:

1) Users - `/api/users`
- POST /api/users/register — Register a user
  - Sample JSON body:
    {
      "nombre": "Omar",
      "apellido": "Uribe",
      "correo": "omar@example.com",
      "contraseña": "secret123",
      "plan": "gratis"
    }
  - Response: 201 Created with created user info (id, correo, plan).
  - Notes: After registering the service sends a verification email with a token.

- POST /api/users/login — Authenticate and receive JWT
  - Sample JSON body:
    {
      "correo": "omar@example.com",
      "contraseña": "secret123"
    }
  - Response: 200 OK with token, tokenType (Bearer), expiresIn information.
  - Errors: 401 Unauthorized if credentials invalid; 403 Forbidden if account inactive/blocked.

- GET /api/users — List all users (requires Authorization: Bearer {token})
- GET /api/users/{id} — Get detailed user (returns `UserDetailDto` with instances)
- PUT /api/users/{id} — Update user data (if plan changes, triggers notifications)
- PATCH /api/users/{id}/status — Toggle user active/inactive
- GET /api/users/verify-email?token={token} — Verify email using token (anonymous endpoint)
- POST /api/users/forgot-password — Request password reset (returns success always for security)
- POST /api/users/reset-password — Reset password using token
- POST /api/users/change-password — Change password (requires JWT and current password)

2) Database Instances - `/api/databaseinstances`
- GET /api/databaseinstances — Returns the authenticated user's instances
- POST /api/databaseinstances — Create a new instance
  - Body: `DatabaseInstanceCreateDto` (fields depend on DTO; typically `Motor`, `Nombre` optional or auto-generated)
  - Behaviour: Service validates plan limits (see `PlanLimits`), generates credentials, calls `DatabaseCreator` to provision, stores the instance and emails the user.
- DELETE /api/databaseinstances/{id} — Deletes instance if the instance belongs to the authenticated user

3) Payments - `/api/payments`
- POST /api/payments/subscribe — Create a subscription preference (requires JWT)
  - Body: { "plan": "intermedio" }
  - Response: initPointUrl (redirect user to MP checkout)
- POST /api/payments/one-time-payment — Create a one-time payment preference
- POST /api/payments/webhook — Public webhook endpoint used by Mercado Pago to post notifications
  - Important: The controller logs raw body and returns 200 OK for handled notifications to avoid re-delivery.
- POST /api/payments/test-webhook — Simulate webhook processing (requires JWT) useful for local testing

4) Audit Logs - `/api/auditlogs`
- GET /api/auditlogs — Return all audit logs
- GET /api/auditlogs/user/{userId} — Return logs for a specific user

5) Health checks - `/api/health/instances`
- GET /api/health/instances — Iterates registered instances and attempts to open a connection per engine (MySQL, PostgreSQL, SQL Server, MongoDB). Returns an object with overall status, per-instance state and messages.

Typical flow (register → verify email → login → create instance → subscribe):
1. Register: POST /api/users/register
2. Verify email: visit or call GET /api/users/verify-email?token={token}
3. Login: POST /api/users/login → copy JWT from response
4. Create instance: POST /api/databaseinstances with Authorization: Bearer {token}
5. Subscribe: POST /api/payments/subscribe with Authorization: Bearer {token}
6. Complete payment at Mercado Pago and let the webhook update the subscription in the API

---

## 7. Code structure (key services and controllers)

Detailed responsibilities (mapping to concrete files):

- `UserService.cs` — Handles user lifecycle: register, login, JWT generation, token-based verification, password reset flows. Uses `PasswordHasher` for password hashing and `TokenGenerator` to create verification/reset tokens. Reads JWT config (`Jwt:Key`, `Issuer`, `Audience`, `ExpirationInMinutes`) to sign tokens.

- `EmailService.cs` — Centralized HTML email templates and SMTP sending. Important methods:
  - `SendEmailVerificationAsync(email, name, token)` — Sends verification email with frontend link.
  - `SendPasswordResetAsync(email, name, token)` — Sends reset link.
  - `SendPaymentConfirmationAsync(...)` — Sends payment confirmation and triggers Discord notifications.
  - Internally calls `SendEmailAsync(...)` which uses `SmtpClient` configured from `EmailSettings`.

- `MercadoPagoService.cs` — Responsible for integrating with Mercado Pago REST API:
  - `CreateOneTimePaymentAsync(userId, plan)` — Builds preference and posts to `/checkout/preferences`.
  - `CreateSubscriptionAsync(userId, plan)` — Builds recurring checkout and saves `Subscription` locally.
  - `ProcessPaymentNotificationAsync(notification)` — Processes incoming webhook notifications, queries MP for payment/subscription details, persists to `Payments`/`Subscriptions` and updates user's plan if payment succeeds.
  - `GetPlanConfiguration(plan)` — Returns plan id and price used when creating preferences.

- `DiscordWebhookService.cs` — Posts embed-based notifications to multiple webhook URLs configured in `DiscordWebhookSettings`. Methods include `SendUserCreatedAsync`, `SendDatabaseCreatedAsync`, `SendPaymentCreatedAsync`, `SendPlanUpdatedAsync`, `SendErrorAsync`, etc.

- `DatabaseInstanceService.cs` — Validates allowed engines (`PlanLimits.MotoresPermitidos`), enforces plan limits (`PlanLimits.MaxPerMotor`), generates unique names and credentials, calls `DatabaseCreator.CrearInstanciaRealAsync` to actually provision the DB and persists a `DatabaseInstance` record.

Flow example (payment webhook):
1. Mercado Pago posts JSON to `/api/payments/webhook`.
2. `PaymentsController.Webhook` reads the raw body, deserializes to `PaymentNotification` and calls `MercadoPagoService.ProcessPaymentNotificationAsync`.
3. Service validates or fetches payment details from MP, persists a `Payment` record or updates its status, updates `User` plan if payment approved, triggers `EmailService` and `DiscordWebhookService` notifications.

---

## 8. Troubleshooting and common errors

Common issues and practical fixes:

- SMTP errors / Emails not delivered:
  - Check `EmailSettings` in `appsettings.json`.
  - If using Gmail, ensure 2FA is active and use an App Password.
  - Confirm port and TLS settings (smtp.gmail.com:587, EnableSsl = true).
  - Inspect Discord "Email Sent" webhook messages for failures logged by the system.

- Webhook not received or repeated retries from Mercado Pago:
  - Ensure `WebhookBaseUrl` points to a publicly reachable URL.
  - Ensure your endpoint responds with HTTP 200 for processed messages. Returning 500 causes re-delivery.
  - Inspect raw webhook payload in logs (PaymentsController logs body) to debug payload mismatches.

- Invalid or unauthorized requests (401/403):
  - Validate JWT configuration: `Jwt:Key`, `Issuer`, `Audience`.
  - Ensure Authorization header is `Bearer <token>`.
  - Check user state: `EmailVerified` and `IsActive` flags in `User` entity.

- Errors provisioning instances:
  - Verify `ConnectionStrings` and `Hosts` configuration used by `DatabaseCreator`.
  - Check plan limits in `PlanLimits.cs` and quotas implemented in `DatabaseInstanceService`.

Debugging tips:
- Increase logging level in `appsettings.Development.json`.
- Use `/api/payments/test-webhook` to simulate MP notifications locally (requires JWT).
- Use Postman or ngrok to expose local dev server for webhook testing.

---

## 9. Customization and deployment

How to change runtime configuration:
- Ports and URLs: `ASPNETCORE_URLS` or `launchSettings.json`.
- Secrets: use environment variables or a secrets manager instead of committing `appsettings.json`.
- Database provider: change `ConnectionStrings` and adjust provider packages if you switch primary DB.

Docker quick start:

- Build image:

```bash
docker build -t crudcloud-api .
```

- Run container (example):

```bash
docker run -e ASPNETCORE_ENVIRONMENT=Production -p 80:80 -p 443:443 crudcloud-api
```

- With compose:

```bash
docker-compose up --build
```

Deployment notes:
- For Azure App Service, publish the project and set application settings for secrets.
- Ensure HTTPS, secure JWT keys, rotate tokens and use a secrets store (Key Vault, AWS Secrets Manager).

---

## 10. License and contact

- No LICENSE file detected. Add `LICENSE` (for example MIT) if you plan to publish the repository.
- Contact: `omaruribe0609@gmail.com` (email found in `appsettings.json` example) — use as maintainer/contact reference.

---

Fin del README.
