-- =====================================================================
-- TurnoMolar - Datos de prueba para todas las clases del dominio
-- =====================================================================
-- Cómo usarlo:
--   1. Iniciar la aplicación una vez (Frontend.MVC o WebAPI) para que cree la base.
--   2. Abrir este archivo en SQL Server Management Studio y ejecutarlo (F5).
-- BORRA todos los datos de las tablas y carga un juego de datos completo y coherente.
-- Se puede correr las veces que se quiera. Las fechas de los turnos se calculan desde
-- el día en que se corre: siempre hay turnos pasados, de hoy y futuros.
--
-- Usuarios (tipo DNI):
--   Responsable: 28456789 / resp123      Admin: 20000000 / admin123
--   Odontólogos: 28456789, 30123456, 26789012, 31555666 / doc123 (25444333 está inhabilitado)
--   Pacientes:   34567890, 38999111, 29888777, 36777888, 43888999, 45123456, 22111000 / paciente123
--                40111222, 27333444, 42555666 / paciente123 (tienen deuda pendiente)
-- =====================================================================

USE [clinicakarina_local];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET DATEFIRST 1; -- lunes = 1

DECLARE @hoy date = CAST(GETDATE() AS date);
DECLARE @wd int = DATEPART(weekday, @hoy);

BEGIN TRANSACTION;

-- Limpieza (de las tablas hijas a las padres)
UPDATE Turnos SET NroTurnoOriginal = NULL;
DELETE FROM Valoraciones;
DELETE FROM DetallesInsumosUtilizados;
DELETE FROM Pagos;
DELETE FROM AtencionesOdontologicas;
DELETE FROM ComprobantesTurnos;
DELETE FROM Turnos;
DELETE FROM HistoriasClinicas;
DELETE FROM Pacientes;
DELETE FROM DisponibilidadesHorarias;
DELETE FROM Convenios;
DELETE FROM Odontologos;
DELETE FROM ResponsablesClinica;
DELETE FROM ObrasSociales;
DELETE FROM Insumos;
DELETE FROM Especialidades;

-- Especialidades: CUF12. Periodoncia está deshabilitada (alt 1.d).
SET IDENTITY_INSERT Especialidades ON;
INSERT INTO Especialidades (IdEspecialidad, Nombre, ArancelParticular, EstadoEspecialidad) VALUES
    (1, N'Odontología General', 15000, N'HABILITADA'),
    (2, N'Endodoncia', 35000, N'HABILITADA'),
    (3, N'Ortodoncia', 50000, N'HABILITADA'),
    (4, N'Cirugía e Implantes', 80000, N'HABILITADA'),
    (5, N'Odontopediatría', 12000, N'HABILITADA'),
    (6, N'Periodoncia', 30000, N'DESHABILITADA');
SET IDENTITY_INSERT Especialidades OFF;

-- ObrasSociales: CUF06. PAMI está dada de baja (No Disponible).
INSERT INTO ObrasSociales (IdentificadorOS, NombreOS, PlanCobertura, EstadoOS) VALUES
    (N'OSDE', N'OSDE Binario', N'Plan 210 / 310', N'DISPONIBLE'),
    (N'SWISS', N'Swiss Medical', N'Black / Gold', N'DISPONIBLE'),
    (N'IOMA', N'IOMA', N'Afiliados Obligatorios', N'DISPONIBLE'),
    (N'PAMI', N'PAMI', N'Jubilados y Pensionados', N'NO DISPONIBLE');

-- Convenios
INSERT INTO Convenios (IdentificadorOS, IdEspecialidad, ArancelConvenio) VALUES
    (N'OSDE', 1, 18000),
    (N'OSDE', 2, 40000),
    (N'OSDE', 3, 58000),
    (N'OSDE', 4, 90000),
    (N'OSDE', 5, 14000),
    (N'SWISS', 1, 20000),
    (N'SWISS', 2, 45000),
    (N'SWISS', 3, 62000),
    (N'SWISS', 4, 95000),
    (N'SWISS', 5, 16000),
    (N'IOMA', 1, 12000),
    (N'IOMA', 2, 28000),
    (N'IOMA', 3, 42000),
    (N'IOMA', 4, 65000),
    (N'IOMA', 5, 10000),
    (N'PAMI', 1, 9000),
    (N'PAMI', 5, 8000);

-- Insumos: CUF09. La amalgama está deshabilitada (alt 3.c) pero se usó en una atención anterior.
SET IDENTITY_INSERT Insumos ON;
INSERT INTO Insumos (IdInsumo, Nombre, CostoUnitario, StockDisponible, EstadoInsumo) VALUES
    (1, N'Kit de Anestesia Local (Mepivacaína)', 2500, 115, N'HABILITADO'),
    (2, N'Resina Compuesta Fotocurable', 6800, 45, N'HABILITADO'),
    (3, N'Película Radiográfica Periapical', 1500, 198, N'HABILITADO'),
    (4, N'Guantes de Látex Descartables (Par)', 400, 498, N'HABILITADO'),
    (5, N'Babero y Eyector Descartable', 300, 349, N'HABILITADO'),
    (6, N'Pasta para Profilaxis Dental', 1200, 59, N'HABILITADO'),
    (7, N'Conos de Gutapercha Endodoncia', 4500, 25, N'HABILITADO'),
    (8, N'Hilo de Sutura Reabsorbible', 3200, 22, N'HABILITADO'),
    (9, N'Amalgama de Plata (Cápsula)', 2000, 0, N'DESHABILITADO');
SET IDENTITY_INSERT Insumos OFF;

-- ResponsablesClinica: Clave: resp123 (Karina) y admin123 (Administrador).
INSERT INTO ResponsablesClinica (TipoDocumento, NroDocumento, Nombre, Apellido, FechaNacimiento, Telefono, Email, Domicilio, Clave, SaltClave, FechaAlta, Rol) VALUES
    (N'DNI', N'28456789', N'Karina', N'González', N'1980-05-14', N'341-4567890', N'karina.gonzalez@turnomolar.com', N'Bv. Oroño 1234, Rosario', N'aY1fDRHleYjAcnrSjbHBVgYJXvOQ08BQShQ8GnOgGLs=', N'YJbdnEK1zUpac4L8GVxmmw==', N'2024-01-02 09:00:00', N'ResponsableClinica'),
    (N'DNI', N'20000000', N'Administrador', N'General', N'1985-01-01', N'341-0000000', N'admin@turnomolar.com', N'Sede Central, Rosario', N'NLKr6/u7G1Mlk4kbDHPSCtPnfrWneY/nqgduV0BzSgk=', N'gxu+Ssxh5TN5fINlQxU9Sw==', N'2024-01-02 09:00:00', N'Admin');

-- Odontologos: CUF04. Clave de todos: doc123. Ricardo Paz está inhabilitado (alt 5.d).
INSERT INTO Odontologos (Matricula, EstadoOdontologo, TipoDocumento, NroDocumento, Nombre, Apellido, FechaNacimiento, Telefono, Email, Domicilio, Clave, SaltClave, FechaAlta, Rol) VALUES
    (N'MP 3840', N'HABILITADO', N'DNI', N'28456789', N'Karina', N'González', N'1980-05-14', N'341-4567890', N'karina.gonzalez@turnomolar.com', N'Bv. Oroño 1234, Rosario', N'eXL/quHehN2ikOMUkg6Q2rO7SjoXu69wE4pJOtcm3Q8=', N'qFepK0oZfCwx2d97nsyS+w==', N'2024-01-02 09:00:00', N'Odontologo'),
    (N'MP 4512', N'HABILITADO', N'DNI', N'30123456', N'Elena', N'Silva', N'1983-08-22', N'341-5678901', N'elena.silva@turnomolar.com', N'Av. Pellegrini 850, Rosario', N'6+4Lx8hBN+7Izm+FI+ng/CgMr4z1iWhUrqGA3aGbpXo=', N'Z/7BHTsLVlOIyDj6PrtuVA==', N'2024-01-02 09:00:00', N'Odontologo'),
    (N'MP 5120', N'HABILITADO', N'DNI', N'26789012', N'Martín', N'López', N'1978-11-30', N'341-6789012', N'martin.lopez@turnomolar.com', N'Santa Fe 2100, Rosario', N'vvozV9WCvGB75KGepBmDQnwOdcvBoe1ENDjagTI2DWs=', N'AlnFPkQhxgYkHV5PNvOphA==', N'2024-01-02 09:00:00', N'Odontologo'),
    (N'MP 6233', N'HABILITADO', N'DNI', N'31555666', N'Sofía', N'Benítez', N'1990-03-08', N'341-7891234', N'sofia.benitez@turnomolar.com', N'Mitre 455, Rosario', N'sAyz2mfzv457jRwzUU7V8fEGR6ci/X62BSU7cgfmb3A=', N'YsFyLlEL4Q9jG8xcyV2ueA==', N'2024-06-10 09:00:00', N'Odontologo'),
    (N'MP 2987', N'INHABILITADO', N'DNI', N'25444333', N'Ricardo', N'Paz', N'1970-07-19', N'341-2223344', N'ricardo.paz@turnomolar.com', N'Mendoza 1980, Rosario', N'jGJmm1d8KSeJ4JwSpdMTB2Zw5nicSuKNDG0pi8By38k=', N'jSkQcwJnz9jzB3lCSUMYFA==', N'2024-01-02 09:00:00', N'Odontologo');

-- DisponibilidadesHorarias: Lunes a viernes (Minuta E1).
INSERT INTO DisponibilidadesHorarias (TipoDocumentoOdontologo, NroDocumentoOdontologo, DiaSemana, HoraInicio, HoraFin, IdEspecialidad) VALUES
    (N'DNI', N'28456789', N'Lunes', N'08:00:00', N'13:00:00', 1),
    (N'DNI', N'28456789', N'Martes', N'08:00:00', N'13:00:00', 1),
    (N'DNI', N'28456789', N'Viernes', N'09:00:00', N'18:00:00', 3),
    (N'DNI', N'30123456', N'Lunes', N'14:00:00', N'19:00:00', 1),
    (N'DNI', N'30123456', N'Miércoles', N'14:00:00', N'19:00:00', 2),
    (N'DNI', N'26789012', N'Martes', N'14:00:00', N'19:00:00', 4),
    (N'DNI', N'26789012', N'Jueves', N'09:00:00', N'18:00:00', 3),
    (N'DNI', N'31555666', N'Miércoles', N'08:00:00', N'13:00:00', 5),
    (N'DNI', N'31555666', N'Jueves', N'08:00:00', N'13:00:00', 1),
    (N'DNI', N'25444333', N'Viernes', N'08:00:00', N'12:00:00', 4);

-- Pacientes: CUF01/CUF02. Clave de todos: paciente123. Ana, Jorge y Lucía tienen deuda (inhabilitados).
INSERT INTO Pacientes (EstadoPaciente, IdentificadorOS, MontoAdeudado, TipoDocumento, NroDocumento, Nombre, Apellido, FechaNacimiento, Telefono, Email, Domicilio, Clave, SaltClave, FechaAlta, Rol) VALUES
    (N'HABILITADO', N'OSDE', 0, N'DNI', N'34567890', N'Manuel', N'Fernández', N'1989-04-15', N'341-3334455', N'manuel.fer@email.com', N'Córdoba 1540, Rosario', N'CTn4CXuilwM+/eh/xD3ulrbFC2EEhml52ifqZnraX5M=', N'QeQoiGUtIz9yrrweas8Jnw==', N'2024-01-15 10:00:00', N'Paciente'),
    (N'HABILITADO', N'SWISS', 0, N'DNI', N'38999111', N'Laura', N'Gómez', N'1995-09-28', N'341-8889900', N'laura.gomez@gmail.com', N'Rioja 2230, Rosario', N'xX0KupcCwM/2qN20srLW7PO7TKsiN+WHe87wNIYwTzk=', N'dB+wReaNI8AE0hMr0Zl+Fg==', N'2024-03-20 10:00:00', N'Paciente'),
    (N'HABILITADO', NULL, 0, N'DNI', N'29888777', N'Carlos', N'Rossi', N'1982-12-10', N'341-1112233', N'carlos.rossi@hotmail.com', N'San Lorenzo 890, Rosario', N'0Yb00gN8o8m3rTvrOE4Uc+OXYqySRuo2lTjiQR7+zzQ=', N'GTla1xLiV4jc8t1ZKmDGGA==', N'2024-05-10 10:00:00', N'Paciente'),
    (N'INHABILITADO', N'IOMA', 12000, N'DNI', N'40111222', N'Ana', N'Martínez', N'1998-02-03', N'341-4445566', N'ana.martinez@gmail.com', N'Entre Ríos 1020, Rosario', N'RDrBFQX904plIshEesZ4ERkjigi/xW/BIrsYcxU/5Zk=', N'SSSV9FoDSj6chSnQOSQ6sg==', N'2024-08-01 10:00:00', N'Paciente'),
    (N'INHABILITADO', NULL, 15000, N'DNI', N'27333444', N'Jorge', N'Díaz', N'1975-06-21', N'341-5556677', N'jorge.diaz@yahoo.com', N'Urquiza 3100, Rosario', N'jBYLTWc8+wNqhZ0MWz45gzOPq9NNui1GFzbp6tE1qPA=', N'9NngTBsl4LUcAwF2HZcfCA==', N'2024-08-12 10:00:00', N'Paciente'),
    (N'INHABILITADO', N'OSDE', 21000, N'DNI', N'42555666', N'Lucía', N'Herrera', N'2000-10-11', N'341-6667788', N'lucia.herrera@gmail.com', N'Corrientes 640, Rosario', N'WIizlc2xm2jsw9u9GvDaygoPNSSKQ6/ivUoADMQ8JsI=', N'F/HKA7stkFbPO3chmGXn6Q==', N'2024-09-05 10:00:00', N'Paciente'),
    (N'HABILITADO', NULL, 0, N'DNI', N'36777888', N'Pedro', N'Suárez', N'1992-01-30', N'341-7778899', N'pedro.suarez@outlook.com', N'Maipú 870, Rosario', N'7V/+JmAOFXMQQBv44/58uweXCcTycgwr7RWn11KP/To=', N'r8EIWdoI/QkYjGZAApsTOA==', N'2025-02-14 10:00:00', N'Paciente'),
    (N'HABILITADO', N'SWISS', 0, N'DNI', N'43888999', N'Valentina', N'Ruiz', N'1997-05-17', N'341-8881122', N'valentina.ruiz@gmail.com', N'Pasco 1450, Rosario', N'KZMQXZulpy+OlwMrge5DEbuvI1I6vF9ieB/y8bglcEI=', N'ZR9B6OXjM3MLWfDbJ+IEWg==', N'2025-03-03 10:00:00', N'Paciente'),
    (N'HABILITADO', NULL, 0, N'DNI', N'45123456', N'Tomás', N'Acosta', N'2015-11-02', N'341-9993344', N'familia.acosta@gmail.com', N'Balcarce 520, Rosario', N'E+bjB2ghq/Q85MlWAOF2Ddx7PPjqBFrMcyGchS2WWzE=', N'O+zyh2/oV5r1b+e7EzzHLg==', N'2025-04-22 10:00:00', N'Paciente'),
    (N'HABILITADO', N'PAMI', 0, N'DNI', N'22111000', N'Rosa', N'Molina', N'1950-08-25', N'341-1230000', N'rosa.molina@gmail.com', N'Laprida 1200, Rosario', N'6WxrndHiZgqqeMDBz8p6DwwSpu0P5QL6HcOgWznDOvY=', N'q4EKDE14coYT95wGZtZ9Xg==', N'2024-02-01 10:00:00', N'Paciente');

-- HistoriasClinicas
SET IDENTITY_INSERT HistoriasClinicas ON;
INSERT INTO HistoriasClinicas (NroHC, FechaCreacion, TipoDocumentoPaciente, NroDocumentoPaciente, AntecedentesMedicos, Alergias, ObservacionesGeneral) VALUES
    (1, N'2024-01-15', N'DNI', N'34567890', N'Sin antecedentes relevantes', N'Ninguna', N'Paciente apto para ortodoncia y limpiezas regulares'),
    (2, N'2024-03-20', N'DNI', N'38999111', N'Hipertensión leve controlada', N'Penicilina', N'Requiere control semestral'),
    (3, N'2024-05-10', N'DNI', N'29888777', N'Bruxismo', N'Ninguna', N'Usa placa miorrelajante nocturna'),
    (4, N'2024-08-01', N'DNI', N'40111222', N'Asma', N'Ninguna', NULL),
    (5, N'2024-08-12', N'DNI', N'27333444', N'Diabetes tipo 2', N'Ibuprofeno', N'Controlar glucemia antes de cirugías'),
    (6, N'2024-09-05', N'DNI', N'42555666', NULL, NULL, NULL),
    (7, N'2025-02-14', N'DNI', N'36777888', N'Sin antecedentes relevantes', N'Látex', N'Usar guantes de nitrilo'),
    (8, N'2025-03-03', N'DNI', N'43888999', N'Sin antecedentes relevantes', N'Ninguna', N'Evaluación para implante'),
    (9, N'2025-04-22', N'DNI', N'45123456', N'Paciente pediátrico', N'Ninguna', N'Concurre con su madre'),
    (10, N'2024-02-01', N'DNI', N'22111000', N'Hipertensión, anticoagulada', N'Aspirina', N'Consultar al cardiólogo antes de extracciones');
SET IDENTITY_INSERT HistoriasClinicas OFF;

-- Turnos: Un turno en cada estado de la ME: Reservado, Presente, Atención Registrada, Finalizado, Ausente, Cancelado y Reprogramado. Las fechas se calculan desde el día en que se corre el script.
SET IDENTITY_INSERT Turnos ON;
INSERT INTO Turnos (NroTurno, ModalidadPagoElegida, FechaHoraTurno, FechaHoraReprogramacion, FechaHoraCancelacion, MotivoCancelacion, EstadoTurno, DescripcionMaterial, ArancelPenalizacionAplicado, TipoDocumentoPaciente, NroDocumentoPaciente, TipoDocumentoOdontologo, NroDocumentoOdontologo, IdEspecialidad, NroTurnoOriginal) VALUES
    (1, N'OBRA_SOCIAL', DATEADD(minute, 540, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 14, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'FINALIZADO', NULL, NULL, N'DNI', N'34567890', N'DNI', N'28456789', 1, NULL),
    (2, N'OBRA_SOCIAL', DATEADD(minute, 900, CAST(DATEADD(day, -(((@wd - 3 + 6) % 7) + 1) - 7, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'FINALIZADO', NULL, NULL, N'DNI', N'38999111', N'DNI', N'30123456', 2, NULL),
    (3, N'PARTICULAR', DATEADD(minute, 600, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 21, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'FINALIZADO', NULL, NULL, N'DNI', N'29888777', N'DNI', N'26789012', 3, NULL),
    (4, N'PARTICULAR', DATEADD(minute, 660, CAST(DATEADD(day, -(((@wd - 2 + 6) % 7) + 1) - 0, @hoy) AS datetime2(0))), NULL, DATEADD(minute, 540, CAST(DATEADD(day, -(((@wd - 2 + 6) % 7) + 1) - 0 + (-3), @hoy) AS datetime2(0))), N'Viaje de trabajo', N'CANCELADO', NULL, NULL, N'DNI', N'29888777', N'DNI', N'28456789', 1, NULL),
    (5, N'OBRA_SOCIAL', DATEADD(minute, 600, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 0, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'AUSENTE', NULL, 12000, N'DNI', N'40111222', N'DNI', N'28456789', 1, NULL),
    (6, N'PARTICULAR', DATEADD(minute, 900, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 0, @hoy) AS datetime2(0))), NULL, DATEADD(minute, 720, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 0, @hoy) AS datetime2(0))), N'Problema personal', N'CANCELADO', NULL, 15000, N'DNI', N'27333444', N'DNI', N'30123456', 1, NULL),
    (7, N'OBRA_SOCIAL', DATEADD(minute, 540, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 0, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'FINALIZADO', NULL, NULL, N'DNI', N'42555666', N'DNI', N'31555666', 1, NULL),
    (8, N'OBRA_SOCIAL', DATEADD(minute, 1170, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 7, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'FINALIZADO', N'Traer estudios radiográficos previos y tomografía.', NULL, N'DNI', N'43888999', N'DNI', N'26789012', 4, NULL),
    (9, N'PARTICULAR', DATEADD(minute, 540, CAST(DATEADD(day, -(((@wd - 3 + 6) % 7) + 1) - 14, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'AUSENTE', NULL, 12000, N'DNI', N'45123456', N'DNI', N'31555666', 5, NULL),
    (10, N'PARTICULAR', DATEADD(minute, 510, CAST(DATEADD(day, -(((@wd - 5 + 6) % 7) + 1) - 35, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'FINALIZADO', NULL, NULL, N'DNI', N'22111000', N'DNI', N'25444333', 4, NULL),
    (11, N'OBRA_SOCIAL', DATEADD(minute, 480, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 49, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'FINALIZADO', NULL, NULL, N'DNI', N'22111000', N'DNI', N'31555666', 1, NULL),
    (12, N'OBRA_SOCIAL', DATEADD(minute, 540, CAST(DATEADD(day, ((1 - @wd + 6) % 7) + 1 + 0, @hoy) AS datetime2(0))), DATEADD(minute, 1080, CAST(DATEADD(day, 0 + (-2), @hoy) AS datetime2(0))), NULL, NULL, N'REPROGRAMADO', NULL, NULL, N'DNI', N'38999111', N'DNI', N'28456789', 1, NULL),
    (13, N'OBRA_SOCIAL', DATEADD(minute, 600, CAST(DATEADD(day, ((2 - @wd + 6) % 7) + 1 + 7, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'RESERVADO', NULL, NULL, N'DNI', N'38999111', N'DNI', N'28456789', 1, 12),
    (14, N'OBRA_SOCIAL', DATEADD(minute, 660, CAST(DATEADD(day, ((4 - @wd + 6) % 7) + 1 + 0, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'RESERVADO', NULL, NULL, N'DNI', N'34567890', N'DNI', N'26789012', 3, NULL),
    (15, N'PARTICULAR', DATEADD(minute, 600, CAST(DATEADD(day, ((3 - @wd + 6) % 7) + 1 + 0, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'RESERVADO', NULL, NULL, N'DNI', N'45123456', N'DNI', N'31555666', 5, NULL),
    (16, N'PARTICULAR', DATEADD(minute, 600, CAST(DATEADD(day, 0, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'PRESENTE', NULL, NULL, N'DNI', N'36777888', N'DNI', N'28456789', 1, NULL),
    (17, N'OBRA_SOCIAL', DATEADD(minute, 540, CAST(DATEADD(day, 0, @hoy) AS datetime2(0))), NULL, NULL, NULL, N'ATENCION_REGISTRADA', NULL, NULL, N'DNI', N'43888999', N'DNI', N'30123456', 2, NULL);
SET IDENTITY_INSERT Turnos OFF;

-- ComprobantesTurnos
SET IDENTITY_INSERT ComprobantesTurnos ON;
INSERT INTO ComprobantesTurnos (NroComprobante, FechaHoraEmision, NroTurno) VALUES
    (1, DATEADD(minute, 720, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 14 + (-7), @hoy) AS datetime2(0))), 1),
    (2, DATEADD(minute, 720, CAST(DATEADD(day, -(((@wd - 3 + 6) % 7) + 1) - 7 + (-7), @hoy) AS datetime2(0))), 2),
    (3, DATEADD(minute, 720, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 21 + (-7), @hoy) AS datetime2(0))), 3),
    (4, DATEADD(minute, 720, CAST(DATEADD(day, -(((@wd - 2 + 6) % 7) + 1) - 0 + (-7), @hoy) AS datetime2(0))), 4),
    (5, DATEADD(minute, 720, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 0 + (-7), @hoy) AS datetime2(0))), 5),
    (6, DATEADD(minute, 720, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 0 + (-7), @hoy) AS datetime2(0))), 6),
    (7, DATEADD(minute, 720, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 0 + (-7), @hoy) AS datetime2(0))), 7),
    (8, DATEADD(minute, 720, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 7 + (-7), @hoy) AS datetime2(0))), 8),
    (9, DATEADD(minute, 720, CAST(DATEADD(day, -(((@wd - 3 + 6) % 7) + 1) - 14 + (-7), @hoy) AS datetime2(0))), 9),
    (10, DATEADD(minute, 720, CAST(DATEADD(day, -(((@wd - 5 + 6) % 7) + 1) - 35 + (-7), @hoy) AS datetime2(0))), 10),
    (11, DATEADD(minute, 720, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 49 + (-7), @hoy) AS datetime2(0))), 11),
    (12, DATEADD(minute, 720, CAST(DATEADD(day, 0 + (-10), @hoy) AS datetime2(0))), 12),
    (13, DATEADD(minute, 1080, CAST(DATEADD(day, 0 + (-2), @hoy) AS datetime2(0))), 13),
    (14, DATEADD(minute, 540, CAST(DATEADD(day, 0 + (-1), @hoy) AS datetime2(0))), 14),
    (15, DATEADD(minute, 540, CAST(DATEADD(day, 0 + (-1), @hoy) AS datetime2(0))), 15),
    (16, DATEADD(minute, 720, CAST(DATEADD(day, 0 + (-6), @hoy) AS datetime2(0))), 16),
    (17, DATEADD(minute, 720, CAST(DATEADD(day, 0 + (-6), @hoy) AS datetime2(0))), 17);
SET IDENTITY_INSERT ComprobantesTurnos OFF;

-- AtencionesOdontologicas
SET IDENTITY_INSERT AtencionesOdontologicas ON;
INSERT INTO AtencionesOdontologicas (IdAtencion, FechaHoraAtencionInicio, FechaHoraAtencionFin, Observaciones, ArancelAplicado, NroTurno, NroHC) VALUES
    (1, DATEADD(minute, 540, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 14, @hoy) AS datetime2(0))), DATEADD(minute, 580, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 14, @hoy) AS datetime2(0))), N'Limpieza y control. Se indica cepillado interdental.', 18000, 1, 1),
    (2, DATEADD(minute, 900, CAST(DATEADD(day, -(((@wd - 3 + 6) % 7) + 1) - 7, @hoy) AS datetime2(0))), DATEADD(minute, 970, CAST(DATEADD(day, -(((@wd - 3 + 6) % 7) + 1) - 7, @hoy) AS datetime2(0))), N'Tratamiento de conducto pieza 36, primera sesión.', 45000, 2, 2),
    (3, DATEADD(minute, 600, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 21, @hoy) AS datetime2(0))), DATEADD(minute, 650, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 21, @hoy) AS datetime2(0))), N'Control de ortodoncia, ajuste de arco.', 50000, 3, 3),
    (4, DATEADD(minute, 540, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 0, @hoy) AS datetime2(0))), DATEADD(minute, 570, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 0, @hoy) AS datetime2(0))), N'Consulta por dolor, se toman dos radiografías.', 18000, 7, 6),
    (5, DATEADD(minute, 1170, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 7, @hoy) AS datetime2(0))), DATEADD(minute, 1245, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 7, @hoy) AS datetime2(0))), N'Extracción de pieza 48 con colgajo. Control en 7 días.', 95000, 8, 8),
    (6, DATEADD(minute, 510, CAST(DATEADD(day, -(((@wd - 5 + 6) % 7) + 1) - 35, @hoy) AS datetime2(0))), DATEADD(minute, 570, CAST(DATEADD(day, -(((@wd - 5 + 6) % 7) + 1) - 35, @hoy) AS datetime2(0))), N'Extracción simple pieza 26 y obturación pieza 25.', 80000, 10, 10),
    (7, DATEADD(minute, 480, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 49, @hoy) AS datetime2(0))), DATEADD(minute, 510, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 49, @hoy) AS datetime2(0))), N'Control general.', 9000, 11, 10),
    (8, DATEADD(minute, 540, CAST(DATEADD(day, 0, @hoy) AS datetime2(0))), DATEADD(minute, 590, CAST(DATEADD(day, 0, @hoy) AS datetime2(0))), N'Endodoncia pieza 21, se coloca medicación intraconducto.', 45000, 17, 8);
SET IDENTITY_INSERT AtencionesOdontologicas OFF;

-- DetallesInsumosUtilizados
INSERT INTO DetallesInsumosUtilizados (IdAtencion, IdInsumo, CantidadUtilizada, CostoUnitarioAlMomento) VALUES
    (1, 1, 1, 2500),
    (1, 4, 2, 400),
    (1, 5, 1, 300),
    (2, 1, 1, 2500),
    (2, 7, 3, 4500),
    (3, 6, 1, 1200),
    (4, 3, 2, 1500),
    (5, 1, 2, 2500),
    (5, 8, 1, 3200),
    (6, 1, 2, 2500),
    (6, 8, 2, 3200),
    (6, 9, 1, 2000),
    (8, 1, 1, 2500),
    (8, 7, 2, 4500);

-- Pagos: Obra social: AporteObraSocial = arancel del convenio, AportePaciente = insumos. El pago 5 regulariza la deuda de Tomás (CUF10).
SET IDENTITY_INSERT Pagos ON;
INSERT INTO Pagos (IdPago, FechaHoraPago, Monto, MetodoPago, AportePaciente, AporteObraSocial, NroTurno, IdentificadorOS) VALUES
    (1, DATEADD(minute, 585, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 14, @hoy) AS datetime2(0))), 21600, N'TARJETA_DEBITO', 3600, 18000, 1, N'OSDE'),
    (2, DATEADD(minute, 975, CAST(DATEADD(day, -(((@wd - 3 + 6) % 7) + 1) - 7, @hoy) AS datetime2(0))), 61000, N'EFECTIVO', 16000, 45000, 2, N'SWISS'),
    (3, DATEADD(minute, 655, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 21, @hoy) AS datetime2(0))), 51200, N'TRANSFERENCIA', NULL, NULL, 3, NULL),
    (4, DATEADD(minute, 1250, CAST(DATEADD(day, -(((@wd - 1 + 6) % 7) + 1) - 7, @hoy) AS datetime2(0))), 103200, N'TARJETA_CREDITO', 8200, 95000, 8, N'SWISS'),
    (5, DATEADD(minute, 660, CAST(DATEADD(day, -(((@wd - 3 + 6) % 7) + 1) - 14 + (5), @hoy) AS datetime2(0))), 12000, N'EFECTIVO', NULL, NULL, 9, NULL),
    (6, DATEADD(minute, 575, CAST(DATEADD(day, -(((@wd - 5 + 6) % 7) + 1) - 35, @hoy) AS datetime2(0))), 93400, N'EFECTIVO', NULL, NULL, 10, NULL),
    (7, DATEADD(minute, 515, CAST(DATEADD(day, -(((@wd - 4 + 6) % 7) + 1) - 49, @hoy) AS datetime2(0))), 9000, N'TRANSFERENCIA', 0, 9000, 11, N'PAMI');
SET IDENTITY_INSERT Pagos OFF;

-- Valoraciones: CUU04.
SET IDENTITY_INSERT Valoraciones ON;
INSERT INTO Valoraciones (IdValoracion, Calificacion, Observaciones, IdAtencion) VALUES
    (1, 5, N'Excelente atención, muy puntual.', 1),
    (2, 4, N'Muy buena atención, un poco de demora.', 3),
    (3, 5, N'Me atendieron fuera de horario por una urgencia. Excelente.', 5),
    (4, 3, N'Buena atención, el consultorio estaba frío.', 6);
SET IDENTITY_INSERT Valoraciones OFF;

COMMIT TRANSACTION;

PRINT 'Datos de prueba de TurnoMolar cargados.';
