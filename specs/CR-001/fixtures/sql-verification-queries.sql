-- Queries de verificación ejecutadas contra dbo.Prueba para capturar línea base (baseline)
-- Ejecutadas en: 2026-07-17
-- Base de datos: IS_WS_PRUEBA (SQL Server 2022, puerto 15533)

-- ============================================================================
-- GATE CRÍTICO: Verificar duplicados preexistentes
-- ============================================================================
-- El CR especifica: Si existen registros activos con Nombre duplicado,
-- el pipeline se DETIENE (política HITL).
-- Resultado esperado: 0 filas

SELECT Nombre, COUNT(*) as Duplicados 
FROM dbo.Prueba 
WHERE Activo = 1 
GROUP BY Nombre 
HAVING COUNT(*) > 1;

-- Resultado capturado: 0 filas ✓ GATE CLEARED

-- ============================================================================
-- Esquema y estructura actual
-- ============================================================================

-- Ver definición de tabla
SELECT 
  TABLE_NAME,
  COLUMN_NAME,
  DATA_TYPE,
  IS_NULLABLE,
  COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Prueba' AND TABLE_SCHEMA = 'dbo'
ORDER BY ORDINAL_POSITION;

-- Ver índices actuales
SELECT 
  i.name AS IndexName,
  i.type_desc AS IndexType,
  STRING_AGG(c.name, ', ') AS Columns
FROM sys.indexes i
JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('[dbo].[Prueba]')
GROUP BY i.name, i.type_desc
ORDER BY i.name;

-- Ver constrains
SELECT 
  CONSTRAINT_NAME,
  CONSTRAINT_TYPE
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
WHERE TABLE_NAME = 'Prueba' AND TABLE_SCHEMA = 'dbo';

-- ============================================================================
-- Datos de seed actuales
-- ============================================================================

SELECT TOP 10 
  Id,
  Nombre,
  Descripcion,
  FechaFundacion,
  Activo,
  FechaActualizacion
FROM dbo.Prueba 
ORDER BY Id;

-- Resultado capturado: 9 registros (seed inicial)

-- ============================================================================
-- Verificación post-operación: Soft delete (Eliminar)
-- Nota: Para usar en fase 4 (regression-tester)
-- ============================================================================

-- Ejecutar DESPUÉS de operación Eliminar sobre id=1
-- SELECT Activo FROM dbo.Prueba WHERE Id = 1;
-- Resultado esperado: 0 (marca como inactivo, no DELETE físico)

-- ============================================================================
-- Verificación post-operación: Crear con fecha+hora
-- Nota: Para usar en fase 4 (regression-tester)
-- ============================================================================

-- Después de Crear con fecha_fundacion="2020-10-15T14:30:00Z"
-- SELECT FechaFundacion FROM dbo.Prueba WHERE Nombre = 'Test Company XYZ';
-- Resultado esperado en BD: 2020-10-15 14:30:00.0000000 (hora preservada)
-- Resultado actual en SOAP response: 2020-10-15 (sin hora - BUG a corregir en CR-001)

-- ============================================================================
-- Análisis: Causa del truncado de hora en fecha_fundacion
-- ============================================================================

-- La BD soporta DATETIME2 (hora completa):
-- DECLARE @dt DATETIME2 = '2020-10-15T14:30:00Z';
-- SELECT @dt; -- Resultado: 2020-10-15 14:30:00.0000000 ✓

-- El problema está en el formateo de salida en PruebaCrudService.cs:264
-- Línea actual (INCORRECTO):
--   ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
-- Línea esperada (CORRECTO):
--   ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)

-- Comparación con fecha_actualizacion (línea 266):
-- fecha_actualizacion ya usa formato correcto "yyyy-MM-ddTHH:mm:ssZ"
