-- Tabla de auditoría para las operaciones Crear/Actualizar/Eliminar sobre dbo.Prueba.
-- Solo almacenamiento: no hay operación SOAP que la consulte (ver CR-001).
-- Sin GO: Flyway (cliente JDBC) no interpreta "GO" como separador de lote
-- (ver specs/CR-001/baseline-snapshot.md, "Comportamiento inesperado encontrado").
CREATE TABLE dbo.PruebaAuditoria
(
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PruebaAuditoria PRIMARY KEY,
    PruebaId    INT           NOT NULL,
    Operacion   VARCHAR(20)   NOT NULL,
    FechaUtc    DATETIME2     NOT NULL
);
