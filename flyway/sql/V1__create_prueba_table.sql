-- Esquema inicial para la entidad PruebaRecord (Domain/Entities/PruebaRecord.cs)
-- consumida por IS_WS_PRUEBA a través de IPruebaRepository.
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'dbo')
BEGIN
    EXEC('CREATE SCHEMA dbo');
END
GO

CREATE TABLE dbo.Prueba
(
    Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Prueba PRIMARY KEY,
    Nombre              NVARCHAR(200)     NOT NULL,
    Descripcion         NVARCHAR(1000)    NOT NULL,
    FechaFundacion      DATETIME2         NOT NULL,
    Activo              BIT               NOT NULL CONSTRAINT DF_Prueba_Activo DEFAULT (1),
    FechaActualizacion  DATETIME2         NOT NULL CONSTRAINT DF_Prueba_FechaActualizacion DEFAULT (SYSUTCDATETIME())
);
GO

CREATE INDEX IX_Prueba_Activo ON dbo.Prueba (Activo);
GO
