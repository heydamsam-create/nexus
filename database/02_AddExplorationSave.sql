-- NEXUS: migración aditiva para guardar el estado del escenario 2D.
-- No elimina ni reconstruye tablas existentes.
USE NexusDB;
GO

IF COL_LENGTH('dbo.EstadoNave', 'EstadoExploracionJson') IS NULL
BEGIN
    ALTER TABLE dbo.EstadoNave
        ADD EstadoExploracionJson NVARCHAR(MAX) NULL;
END;
GO
