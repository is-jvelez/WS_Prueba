-- Tabla de auditoría para operaciones que modifican dbo.Prueba (Crear, Actualizar, Eliminar).
-- Solo almacenamiento: no se expone todavía como operación SOAP consultable.
CREATE TABLE dbo.PruebaAudit
(
    Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PruebaAudit PRIMARY KEY,
    Operacion NVARCHAR(20)      NOT NULL,
    PruebaId  INT               NOT NULL CONSTRAINT FK_PruebaAudit_Prueba REFERENCES dbo.Prueba(Id),
    FechaUtc  DATETIME2         NOT NULL CONSTRAINT DF_PruebaAudit_FechaUtc DEFAULT (SYSUTCDATETIME())
);
GO

CREATE INDEX IX_PruebaAudit_PruebaId ON dbo.PruebaAudit (PruebaId);
GO
