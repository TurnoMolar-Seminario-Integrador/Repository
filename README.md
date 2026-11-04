# TurnoMolar - Sistema de Gestión Odontológica y Turnos

> **Proyecto de Seminario Integrador**
> Sistema integral para la digitalización, agendamiento y gestión de atención odontológica en **Clínica Dental Rosario**.

---

## Índice
1. [Descripción General](#descripción-general)
2. [Objetivos del Proyecto](#objetivos-del-proyecto)
3. [Arquitectura del Sistema](#arquitectura-del-sistema)
4. [Estructura del Proyecto](#estructura-del-proyecto)
5. [Casos de Uso Principales (CUU)](#casos-de-uso-principales-cuu)
6. [Stack Tecnológico](#stack-tecnológico)
7. [Modelo de Datos](#modelo-de-datos)
8. [Instalación y Puesta en Marcha](#instalación-y-puesta-en-marcha)
9. [Configuración de Base de Datos](#configuración-de-base-de-datos)
10. [Ejecución del Proyecto](#ejecución-del-proyecto)
11. [Pruebas Automatizadas](#pruebas-automatizadas)
12. [Datos de Prueba](#datos-de-prueba-seed-data)
13. [Integrantes del Grupo](#integrantes-del-grupo)
14. [Materia y Contexto Académico](#materia-y-contexto-académico)

---

## Descripción General

**TurnoMolar** es una plataforma web desarrollada en **.NET 8** bajo una **arquitectura en capas (N-Layer Architecture)**, diseñada para modernizar la atención clínica y el autoservicio de pacientes. Permite a los pacientes solicitar turnos en tiempo real visualizando disponibilidad horaria real por profesional y especialidad, descargar comprobantes con código QR, gestionar su estado de cuenta y calificar la atención recibida.

A su vez, busca proporcionar al equipo odontológico y administrativo herramientas para gestionar agendas, historiales clínicos e insumos médicos.

---

## Objetivos del Proyecto

- **Optimización de Agendamiento**: reducción de tiempos de espera mediante un asistente (wizard) paso a paso, con calendario y horarios reales.
- **Detección Temprana de Inhabilitación**: verificación automática del estado financiero del paciente (inhabilitación preventiva en caso de deuda exigible).
- **Trazabilidad Clínica**: registro centralizado de atenciones y consultas en la historia clínica digital.
- **Autonomía del Paciente**: portal para reprogramar turnos, cancelarlos y consultar su estado de cuenta.
- **Interoperabilidad**: separación entre capa de dominio, persistencia, lógica de aplicación, API REST y clientes frontend (MVC y Blazor).

---

## Arquitectura del Sistema

El proyecto implementa una arquitectura en capas orientada al dominio. **Frontend.MVC no pasa por la WebAPI**: referencia `Application.Services` directamente y comparte proceso con él, para no duplicar lógica de negocio entre los dos frontends. La WebAPI expone esa misma capa de servicios por HTTP para consumidores externos (Blazor, apps móviles, terceros).

```
+-------------------------------------------------------------+
|                        PRESENTACIÓN                         |
|   Frontend.MVC (Portal Paciente)   |  Blazor (Panel Admin)  |
+------------------+--------------------------+---------------+
                    | (in-process)             | HTTP / JSON
                    |                +---------v-------------+
                    |                |  WebAPI (RESTful API) |
                    |                |  Controladores, Swagger|
                    |                +---------+-------------+
                    |                          |
+-------------------v--------------------------v---------------+
|              Application.Services (Capa de Negocio)          |
|         Casos de Uso, Validaciones, Lógica Operativa         |
+--------------+------------------------------+----------------+
               |                              |
+--------------v--------------+ +-------------v---------------+
|     DTOs (Data Transfer)    | |   Domain.Model (Entidades)  |
+-----------------------------+ +-------------+---------------+
                                              |
+---------------------------------------------v---------------+
|                     Data (Acceso a Datos)                   |
|         Entity Framework Core 8, DbContext, seed data       |
+------------------------------+------------------------------+
                               | T-SQL / ADO.NET
+------------------------------v------------------------------+
|                     Microsoft SQL Server                    |
+-------------------------------------------------------------+
```

---

## Estructura del Proyecto

| Proyecto / Directorio | Responsabilidad |
| :--- | :--- |
| **`Domain.Model`** | Entidades de negocio (`Paciente`, `Odontologo`, `Turno`, `HistoriaClinica`, `ComprobanteDeTurno`, `Pago`, `ObraSocial`, `Insumo`, etc.) y contratos de dominio. Incluye también algunas clases de una versión anterior del modelo (`Factura`, `Consulta`, `Multa`, `Consultorio`, `HorarioOdont`) que ya no están mapeadas en el DbContext ni en uso — quedan pendientes de limpieza. |
| **`DTOs`** | Objetos de transferencia de datos para el intercambio entre capas. |
| **`Data`** | Configuración de Entity Framework Core 8 (`TurnoMolarDbContext`), repositorios, mapeos Fluent API y seed data (`DbInitializer`). |
| **`Application.Services`** | Implementación de las reglas de negocio, validaciones de disponibilidad y orquestación de casos de uso. |
| **`WebAPI`** | API RESTful documentada con Swagger/OpenAPI, para consumidores externos a Frontend.MVC. |
| **`Frontend.MVC`** | Portal web del paciente en ASP.NET Core MVC. |
| **`Blazor.Server` / `Blazor.WebAssembly`** | Módulos administrativos para recepción y gestión de agendas. |
| **`WebAPI.Tests`** | Proyecto de test scaffoldeado; todavía sin casos de prueba propios del dominio. |

---

## Casos de Uso Principales (CUU)

Estado verificado contra la documentación del proyecto (casos de uso E2/E4, reglas de negocio, máquinas de estado y diccionario de datos). La correspondencia detallada entre cada artefacto UML y el código está en [`docs/TRAZABILIDAD.md`](docs/TRAZABILIDAD.md); las decisiones que se apartan de la documentación, en [`docs/DESVIOS.md`](docs/DESVIOS.md).

| Caso de uso | Actor | Dónde está | Estado |
| :--- | :--- | :--- | :--- |
| CUU01 Agendar turno | Paciente / Responsable | `AgendaTurnoService` | ✅ |
| CUU02 Gestionar asistencia | Responsable | `AsistenciaTurnoService` | ✅ |
| CUU03 Finalizar atención (pago total, sin pagos parciales) | Odontólogo / Responsable | `FinalizarAtencionService` | ✅ |
| CUU04 Valorar atención | Paciente | `ValorarAtencionService` | ✅ |
| CUF01 Incorporar paciente | Paciente / Responsable | `IncorporarPacienteService` | ✅ |
| CUF02 Rectificar datos del paciente | Paciente / Responsable | `RectificarDatosPacienteService` | ✅ |
| CUF03 Administrar historia clínica | Odontólogo | `AdministrarHistoriaClinicaService` | ✅ |
| CUF04 Conformar cuerpo de odontólogos | Responsable | `ConformarCuerpoOdontologosService` — menú **Odontólogos** | ✅ |
| CUF05 Consultar valoraciones | Odontólogo / Responsable | `ConsultarValoracionesService` — **Mis Valoraciones** / **Valoraciones** | ✅ |
| CUF06 Administrar convenios con obras sociales | Responsable | `AdministrarConveniosService` — menú **Obras Sociales** | ✅ |
| CUF07 Cancelar turno | Paciente | `GestionTurnoService` | ✅ |
| CUF08 Reprogramar turno | Paciente | `GestionTurnoService` | ✅ |
| CUF09 Abastecer insumos y materiales | Responsable | `AbastecerInsumosService` — menú **Insumos** | ✅ |
| CUF10 Regularizar deuda | Paciente / Responsable | `RegularizarDeudaService` | ✅ |
| CUF11 Coordinar atención extraordinaria | Responsable | `CoordinarAtencionExtraordinariaService` — menú **Atención Extraordinaria** | ✅ |
| CUF12 Administrar especialidad | Responsable | `AdministrarEspecialidadService` — menú **Especialidades** | ✅ |

**Cierre de jornada:** un turno que sigue `Reservado` cuando termina su día pasa a `Ausente` con la penalización correspondiente y el paciente queda `Inhabilitado` (mismo criterio que CUU02 alt 1.b). Lo hace `VencimientoTurnosService`, que `Frontend.MVC` ejecuta al iniciar y luego cada hora (`CierreJornadaHostedService`).

---

## Stack Tecnológico

- **Lenguaje Principal**: C# 12
- **Framework Base**: .NET 8.0 LTS
- **Motor ORM**: Entity Framework Core 8. El esquema se crea y se siembra automáticamente al arrancar (`DbInitializer`, vía `EnsureCreatedAsync`); el repositorio también incluye una carpeta `Migrations/` para evolución de esquema, pero **no es el mecanismo que usa la app al arrancar hoy**.
- **Base de Datos**: Microsoft SQL Server (instancia local para desarrollo).
- **Frontend MVC**:
  - ASP.NET Core Razor Views
  - HTML5 + CSS3 (variables custom, Grid, Flexbox)
  - JavaScript vanilla (ES6+) para las partes dinámicas del wizard de turnos
  - Bootstrap 5.3 y Bootstrap Icons
  - Tipografía: Plus Jakarta Sans (Google Fonts)
- **Documentación API**: Swagger UI / OpenAPI
- **Testing**: proyectos con xUnit ya referenciados, sin casos de prueba propios todavía.

---

## Modelo de Datos

El esquema relacional activo tiene **15 tablas** (`DbSet`) reales en `TurnoMolarDbContext`:
`Pacientes`, `Odontologos`, `ResponsablesClinica`, `Especialidades`, `ObrasSociales`, `Convenios`, `DisponibilidadesHorarias`, `Turnos`, `ComprobantesTurnos`, `AtencionesOdontologicas`, `HistoriasClinicas`, `Insumos`, `DetallesInsumosUtilizados`, `Pagos`, `Valoraciones`.

```mermaid
erDiagram
    PACIENTE ||--o{ TURNO : reserva
    PACIENTE ||--o| HISTORIA_CLINICA : tiene
    PACIENTE }o--o| OBRA_SOCIAL : afiliado
    ODONTOLOGO ||--o{ TURNO : atiende
    ODONTOLOGO ||--o{ DISPONIBILIDAD_HORARIA : posee
    ESPECIALIDAD ||--o{ TURNO : corresponde
    ESPECIALIDAD ||--o{ DISPONIBILIDAD_HORARIA : requiere
    OBRA_SOCIAL ||--o{ CONVENIO : ofrece
    ESPECIALIDAD ||--o{ CONVENIO : cubre
    TURNO ||--o| COMPROBANTE_DE_TURNO : genera
    TURNO ||--o| ATENCION_ODONTOLOGICA : produce
    TURNO ||--o| PAGO : liquida
    ATENCION_ODONTOLOGICA ||--o| VALORACION : recibe
    ATENCION_ODONTOLOGICA ||--o{ DETALLE_INSUMO_UTILIZADO : usa
    INSUMO ||--o{ DETALLE_INSUMO_UTILIZADO : se_registra_en
    HISTORIA_CLINICA ||--o{ ATENCION_ODONTOLOGICA : contiene
```

---

## Instalación y Puesta en Marcha

### Prerrequisitos
- .NET 8.0 SDK instalado.
- SQL Server (local o accesible en red).
- Visual Studio 2022 / VS Code / JetBrains Rider.

### Clonación del Repositorio
```bash
git clone https://github.com/TurnoMolar-Seminario-Integrador/Repository.git
cd Repository
```

### Restauración de Paquetes NuGet
```bash
dotnet restore DentalClinic.sln
```

### Compilación
```bash
dotnet build DentalClinic.sln
```
Si venís de una compilación anterior con errores raros o dependencias corridas, limpiá antes de compilar (borra los `bin/`/`obj/` generados, no toca el código fuente):
```bash
dotnet clean DentalClinic.sln
dotnet build DentalClinic.sln
```

---

## Configuración de Base de Datos

1. Configurar la cadena de conexión en `WebAPI/appsettings.json` y `Frontend.MVC/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=clinicakarina_local;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```
Si el equipo trabaja contra un servidor remoto compartido, **no subas usuario y contraseña reales a este archivo ni al repositorio**: usá `dotnet user-secrets` o una variable de entorno, y compartí las credenciales por un canal aparte.

2. No hace falta correr migraciones a mano: al iniciar cualquiera de los dos proyectos (`WebAPI` o `Frontend.MVC`), `DbInitializer` crea la base si no existe (`EnsureCreatedAsync`) y siembra los datos iniciales automáticamente.

3. **Si ya tenías la base creada de una versión anterior, borrala y dejá que se vuelva a crear.** `EnsureCreated` no modifica una base existente, y esta versión cambia el esquema: la clave de `DisponibilidadesHorarias` ahora incluye `HoraInicio` (como en el MDF), y los estados por defecto de odontólogo y obra social pasan a `HABILITADO` y `DISPONIBLE`. Por ejemplo, desde SQL Server Management Studio: `DROP DATABASE clinicakarina_local;` y luego iniciar `Frontend.MVC`.

   Las columnas `Especialidades.EstadoEspecialidad` e `Insumos.EstadoInsumo` (CUF12 y CUF09 E4.R02, deshabilitar especialidades e insumos) no requieren borrar la base: `DbInitializer` las agrega al iniciar si faltan, con valor `HABILITADA` / `HABILITADO`.

---

## Ejecución del Proyecto

### 1. Iniciar la API Backend
```bash
cd WebAPI
dotnet run
```
Swagger UI disponible en: `https://localhost:7043/swagger`

### 2. Iniciar el Portal Web Paciente (MVC)
```bash
cd Frontend.MVC
dotnet run
```
Portal del Paciente disponible en: `http://localhost:5247` o `https://localhost:7172`

---

## Pruebas Automatizadas

El proyecto `TurnoMolar.Tests` (xUnit) prueba los servicios de los casos de uso contra una base **SQLite en memoria** con el mismo esquema y los mismos datos iniciales que `DbInitializer`. No necesita SQL Server.

```bash
dotnet test TurnoMolar.Tests
```

Cada clase de prueba lleva el nombre del caso de uso y cada prueba el paso o camino alternativo que cubre (por ejemplo `Alt_3a_rechaza_matricula_repetida`). Hoy cubren CUF04, CUF05, CUF06, CUF09, CUF11, CUF12, el cierre de jornada, CUF07, CUF08, CUF10 y el rechazo del pago parcial de CUU03.

---

## Datos de Prueba (Seed Data)

| Rol | Usuario | DNI | Especialidad / Cobertura |
| :--- | :--- | :--- | :--- |
| **Paciente** | Manuel Fernández (`manuel.fer@email.com`) | `34567890` | `HABILITADO`, con obra social OSDE |
| **Odontóloga** | Dra. Karina González (MP 3840) | `28456789` | Odontología General — Lunes y Martes 8 a 13 hs |
| **Odontóloga** | Dra. Elena Silva (MP 4512) | `30123456` | Endodoncia — Miércoles 14 a 19 hs |
| **Odontólogo** | Dr. Martín López (MP 5120) | `26789012` | Ortodoncia — Jueves 9 a 18 hs |

Contraseñas del seed: responsable `resp123` (DNI `28456789`), admin `admin123`, odontólogos `doc123`, pacientes `paciente123`. Un odontólogo dado de alta desde **Odontólogos** (CUF04) entra con su número de documento como contraseña inicial.

> Cirugía e Implantes y Odontopediatría todavía no tienen ningún odontólogo ni disponibilidad horaria cargados en el seed — al elegirlas en el wizard de turnos no va a haber nada para reservar hasta que se agregue esa disponibilidad.

---

## Integrantes del Grupo

- **Manuel Fernández**
- **Alexis Mateo**
- **Bautista Alfaro**
- **Santiago Martina**

---

## Materia y Contexto Académico

- **Asignatura**: Seminario Integrador
- **Carrera**: Ingeniería en Sistemas de Información / Licenciatura en Sistemas
- **Año lectivo**: 2026
- **Proyecto**: Plataforma de Gestión TurnoMolar para la Clínica Dental Rosario