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


-- Seed de datos para dbo.Prueba
-- Nombre/Descripcion/FechaFundacion son NOT NULL; Activo y FechaActualizacion tienen default,
-- pero los seteo explícito en algunos registros para tener variedad de estados.

INSERT INTO dbo.Prueba (Nombre, Descripcion, FechaFundacion, Activo)
VALUES
    (N'Prueba Alpha',   N'Registro inicial para pruebas de integración del módulo de dominio.', '2018-03-15', 1),
    (N'Prueba Beta',    N'Caso de prueba con descripción larga para validar el límite de NVARCHAR(1000) en escenarios de edición desde la UI.', '2019-07-22', 1),
    (N'Prueba Gamma',   N'Registro histórico marcado como inactivo para validar filtros por Activo.', '2015-01-10', 0),
    (N'Prueba Delta',   N'Registro reciente usado para pruebas de ordenamiento por FechaActualizacion.', '2022-11-05', 1),
    (N'Prueba Epsilon', N'Registro con fecha de fundación futura respecto al resto, para casos borde de validación temporal.', '2024-06-01', 1),
    (N'Prueba Zeta',    N'Registro inactivo con fecha de fundación muy antigua, útil para pruebas de rango de fechas.', '2005-09-30', 0),
    (N'Prueba Eta',     N'Registro con nombre y descripción mínimos para probar longitud límite inferior.', '2020-02-14', 1),
    (N'Prueba Theta',   N'Registro duplicado a propósito en Nombre para validar que no hay constraint UNIQUE en esa columna.', '2021-08-19', 1),
    (N'Prueba Iota',    N'Registro de cierre del seed, usado como ancla para pruebas de paginación (10mo elemento).', '2017-12-25', 0);
GO

 