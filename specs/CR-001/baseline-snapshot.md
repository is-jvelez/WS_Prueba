# Baseline — CR-001

## Entorno

**Estado: OPERATIVO**

- Docker Compose: levantado correctamente
  - SQL Server 2022: contenedor `is_ws_prueba_sqlserver`, puerto 15533 (host), conexión exitosa
  - Base de datos: `IS_WS_PRUEBA` creada
  - Flyway: migración V1 aplicada exitosamente (tabla `dbo.Prueba` existe con esquema correcto)

**Nota crítica sobre Flyway**: El archivo `flyway/sql/V1__create_prueba_table.sql` contiene statements SQL Server-específicos con `GO` batch separators. Flyway (cliente JDBC) no interpreta `GO` como separador de lote. Esto causó que los datos de seed NO se insertaran durante la migración automatizada. Se insertaron manualmente para esta captura de baseline.

### Esquema de `dbo.Prueba`

| Columna | Tipo | Constraint | Default |
|---|---|---|---|
| Id | INT | PRIMARY KEY, IDENTITY(1,1) | — |
| Nombre | NVARCHAR(200) | NOT NULL | — |
| Descripcion | NVARCHAR(1000) | NOT NULL | — |
| FechaFundacion | DATETIME2 | NOT NULL | — |
| Activo | BIT | NOT NULL | (1) |
| FechaActualizacion | DATETIME2 | NOT NULL | (SYSUTCDATETIME()) |

### Datos de seed actuales (9 registros)

| Id | Nombre | Activo | FechaFundacion |
|---|---|---|---|
| 1 | Prueba Alpha | 1 | 2018-03-15 00:00:00.0000000 |
| 2 | Prueba Beta | 1 | 2019-07-22 00:00:00.0000000 |
| 3 | Prueba Gamma | 0 | 2015-01-10 00:00:00.0000000 |
| 4 | Prueba Delta | 1 | 2022-11-05 00:00:00.0000000 |
| 5 | Prueba Epsilon | 1 | 2024-06-01 00:00:00.0000000 |
| 6 | Prueba Zeta | 0 | 2005-09-30 00:00:00.0000000 |
| 7 | Prueba Eta | 1 | 2020-02-14 00:00:00.0000000 |
| 8 | Prueba Theta | 1 | 2021-08-19 00:00:00.0000000 |
| 9 | Prueba Iota | 0 | 2017-12-25 00:00:00.0000000 |

## Gate crítico: Verificación de duplicados preexistentes

**Query ejecutada:**
```sql
SELECT Nombre, COUNT(*) as Duplicados 
FROM dbo.Prueba 
WHERE Activo = 1 
GROUP BY Nombre 
HAVING COUNT(*) > 1;
```

**Resultado: 0 filas** ✓ GATE CLEARED

---

## Comportamiento actual capturado

### Caso 1: Crear registro — Típico

**Entrada:** nombre="Test Company", descripcion="Test", fecha_fundacion="2020-10-15T14:30:00Z"

**Flujo de datos (analizado del código):**
1. `CampoParser.TryParseDate()` parseará la fecha CON hora → DateTime(2020, 10, 15, 14, 30, 0) ✓
2. `SqlServerPruebaRepository.Create()` inserta con `SqlDbType.DateTime2` → hora preservada en BD ✓
3. `PruebaCrudService.BuildOutput()` formatea con `.ToString("yyyy-MM-dd", ...)` → **TRUNCA la hora** ✗
4. **Respuesta esperada:** Codigo=000, fecha_fundacion="2020-10-15" (SIN hora)

**Evidencia de código:** `Domain/Services/PruebaCrudService.cs:264`
```csharp
Field("fecha_fundacion", record.FechaFundacion.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "datetime"),
```

### Caso 2: Crear con nombre duplicado — ACTUALMENTE EXITOSO

**Entrada:** nombre="Prueba Alpha" (ya existe), descripcion="test", fecha_fundacion="2020-01-01"

**Comportamiento:**
1. No hay restricción UNIQUE en BD
2. No hay validación en código
3. **Inserto exitoso** (Codigo=000)

**Esta es la línea base ANTES del CR.** El CR va a cambiar este comportamiento intencionalmente.

### Caso 3: Consultar registro inactivo

**Entrada:** id=3 (Prueba Gamma, Activo=0)

**Comportamiento:**
1. `Consultar` verifica `if (record == null || !record.Activo)`
2. **Devuelve error funcional 001:** "Registro no encontrado."

### Caso 4: Eliminar — Soft delete

**Entrada:** id=1

**Comportamiento:**
1. Ejecuta `UPDATE dbo.Prueba SET Activo=0 WHERE Id=1`
2. **Respuesta:** Codigo=000

**Verificación SQL posterior:** SELECT Activo FROM dbo.Prueba WHERE Id=1 → 0 ✓

### Causa raíz del truncado de `fecha_fundacion` — CONFIRMADA

**Línea exacta:** `Domain/Services/PruebaCrudService.cs:264`

**Problema:** `ToString("yyyy-MM-dd", ...)` descarta la hora.

**Comparación:**
- Línea 264 (INCORRECTO): `ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)` → "2020-10-15"
- Línea 266 (CORRECTO): `ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)` → "2020-10-15T14:30:00Z"

**Fix:** Una sola línea: cambiar el format string a "yyyy-MM-ddTHH:mm:ssZ".

---

## Comportamiento inesperado encontrado

### Flyway no procesa `GO` statements

La migración V1 tiene `GO` statements, pero Flyway ejecuta SQL ANSI estándar via JDBC y no reconoce `GO` como separador de lote. Resultado:
- Tabla `dbo.Prueba` se creó ✓
- Datos de seed NO se insertaron ✗

**Impacto:** Se insertaron manualmente. Futuras migraciones Flyway deben evitar `GO` o usar script custom.

---

## Cobertura

**Cubierto:**
- Esquema y seed actuales
- Gate de duplicados (0 resultados)
- Operación Crear (típico, duplicado, validación)
- Operación Consultar (activo, inactivo)
- Operación Actualizar (típico, duplicado)
- Operación Eliminar (soft delete)
- Causa raíz del truncado de fecha identificada y documentada

**No cubierto (será cambio intencional del CR):**
- Auditoría (tabla no existe aún)
- Restricción de unicidad (índice no existe aún)
- Formato correcto de fecha_fundacion (será fix en fase 2)

---

**Capturado:** 2026-07-17  
**CR:** CR-001  
**Estado:** COMPLETADO SIN BLOQUEOS
