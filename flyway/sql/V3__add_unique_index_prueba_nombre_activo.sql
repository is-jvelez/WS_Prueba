-- Restricción de unicidad de Nombre entre registros activos (Activo = 1), filtrada
-- para no chocar con homónimos inactivos (ver CR-001).
-- Precondición confirmada por behavior-capturer (fase 1): no existen hoy duplicados
-- activos de Nombre (specs/CR-001/baseline-snapshot.md, "Gate crítico").
-- Sin GO: Flyway (cliente JDBC) no interpreta "GO" como separador de lote.
CREATE UNIQUE INDEX UX_Prueba_Nombre_Activo ON dbo.Prueba (Nombre) WHERE Activo = 1;
