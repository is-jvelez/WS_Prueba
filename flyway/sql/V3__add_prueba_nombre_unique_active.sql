-- Evita dos registros activos con el mismo Nombre. Filtrado por Activo=1 para no chocar
-- con registros inactivos que compartan Nombre.
CREATE UNIQUE INDEX UX_Prueba_Nombre_Activo ON dbo.Prueba (Nombre) WHERE Activo = 1;
GO
