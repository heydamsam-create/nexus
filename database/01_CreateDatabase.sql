-- ============================================================================
-- PROYECTO NEXUS - HITO 4: OPERACIÓN MEMORIA
-- Script 01: Creación de Base de Datos y Esquema Relacional (5 Tablas)
-- ============================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'NexusDB')
BEGIN
    CREATE DATABASE NexusDB;
END
GO

USE NexusDB;
GO

-- Limpieza preventiva para re-ejecución limpia
IF OBJECT_ID('dbo.BitacoraMisiones', 'U') IS NOT NULL DROP TABLE dbo.BitacoraMisiones;
IF OBJECT_ID('dbo.DecisionesExpedicion', 'U') IS NOT NULL DROP TABLE dbo.DecisionesExpedicion;
IF OBJECT_ID('dbo.EstadoNave', 'U') IS NOT NULL DROP TABLE dbo.EstadoNave;
IF OBJECT_ID('dbo.Expediciones', 'U') IS NOT NULL DROP TABLE dbo.Expediciones;
IF OBJECT_ID('dbo.Cadetes', 'U') IS NOT NULL DROP TABLE dbo.Cadetes;
GO

-- 1. TABLA CADETES (Perfiles de jugador)
CREATE TABLE dbo.Cadetes (
    IdCadete INT IDENTITY(1,1) PRIMARY KEY,
    NombreCadete NVARCHAR(60) NOT NULL UNIQUE,
    Rango NVARCHAR(40) NOT NULL DEFAULT 'Cadete de Exploración',
    ExpedicionesTotales INT NOT NULL DEFAULT 0,
    MejorPuntuacion INT NOT NULL DEFAULT 0,
    FechaRegistro DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

-- 2. TABLA EXPEDICIONES (Partidas guardadas / Ranuras de guardado)
CREATE TABLE dbo.Expediciones (
    IdExpedicion INT IDENTITY(1,1) PRIMARY KEY,
    IdCadete INT NOT NULL,
    NombreRanura NVARCHAR(80) NOT NULL,
    SectorObjetivo NVARCHAR(60) NOT NULL DEFAULT 'Eridani-9 // Cráter de Eos',
    EtapaActual NVARCHAR(30) NOT NULL DEFAULT 'Briefing',
    EstadoExpedicion NVARCHAR(25) NOT NULL DEFAULT 'EnCurso',
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaUltimoGuardado DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Expediciones_Cadetes FOREIGN KEY (IdCadete)
        REFERENCES dbo.Cadetes(IdCadete) ON DELETE CASCADE,
    CONSTRAINT CK_Expediciones_Estado CHECK (EstadoExpedicion IN ('EnCurso', 'Completada', 'Abandonada'))
);
GO

-- 3. TABLA ESTADO NAVE (Telemetría, recursos y banderas tácticas de MissionGame)
CREATE TABLE dbo.EstadoNave (
    IdEstadoNave INT IDENTITY(1,1) PRIMARY KEY,
    IdExpedicion INT NOT NULL UNIQUE,
    Oxigeno INT NOT NULL DEFAULT 100,
    IntegridadCasco INT NOT NULL DEFAULT 100,
    DatosRecuperados INT NOT NULL DEFAULT 0,
    EscaneoOrbital BIT NOT NULL DEFAULT 0,
    DescensoDirecto BIT NOT NULL DEFAULT 0,
    SenalConfiable BIT NOT NULL DEFAULT 0,
    SondaEnviada BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_EstadoNave_Expediciones FOREIGN KEY (IdExpedicion)
        REFERENCES dbo.Expediciones(IdExpedicion) ON DELETE CASCADE,
    CONSTRAINT CK_EstadoNave_Oxigeno CHECK (Oxigeno BETWEEN 0 AND 100),
    CONSTRAINT CK_EstadoNave_Casco CHECK (IntegridadCasco BETWEEN 0 AND 100),
    CONSTRAINT CK_EstadoNave_Datos CHECK (DatosRecuperados BETWEEN 0 AND 100)
);
GO

-- 4. TABLA DECISIONES EXPEDICIÓN (Bitácora paso a paso de la partida)
CREATE TABLE dbo.DecisionesExpedicion (
    IdDecision INT IDENTITY(1,1) PRIMARY KEY,
    IdExpedicion INT NOT NULL,
    NumeroPaso INT NOT NULL,
    Etapa NVARCHAR(30) NOT NULL,
    OpcionElegida NVARCHAR(120) NOT NULL,
    FechaDecision DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Decisiones_Expediciones FOREIGN KEY (IdExpedicion)
        REFERENCES dbo.Expediciones(IdExpedicion) ON DELETE CASCADE
);
GO

-- 5. TABLA BITÁCORA MISIONES (Historial de misiones finalizadas - reemplaza al JSON)
CREATE TABLE dbo.BitacoraMisiones (
    IdBitacora INT IDENTITY(1,1) PRIMARY KEY,
    IdExpedicion INT NULL,
    NombreMision NVARCHAR(80) NOT NULL DEFAULT 'Operación Umbral de Eos',
    Desenlace NVARCHAR(60) NOT NULL,
    Resumen NVARCHAR(500) NOT NULL,
    PuntuacionTotal INT NOT NULL,
    OxigenoFinal INT NOT NULL,
    CascoFinal INT NOT NULL,
    DatosFinales INT NOT NULL,
    FechaFinalizacion DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Bitacora_Expediciones FOREIGN KEY (IdExpedicion)
        REFERENCES dbo.Expediciones(IdExpedicion) ON DELETE SET NULL
);
GO

-- Datos iniciales de prueba (Seed)
INSERT INTO dbo.Cadetes (NombreCadete, Rango)
VALUES ('Cadete Vance', 'Cadete de Exploración');
GO