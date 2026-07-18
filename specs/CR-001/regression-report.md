# Regression Report — CR-001
## Contexto de Ejecución
**Fecha:** 2026-07-17
**Fase:** 4 - Regression Testing (post-implementación)
**Prerequisitos verificados:**
- Fase 1 (Behavior Capture): COMPLETADA - baseline capturado sin bloqueos
- Fase 2 (Implementación): COMPLETADA - código implementado según spec
- Fase 3 (Unit/Integration Tests): COMPLETADA - 6/6 tests PASANDO

## Análisis de Cambios Implementados

### 1. Fix de fecha_fundacion

**Ubicación:** `Domain/Services/PruebaCrudService.cs:272`
**Cambio:** ToString("yyyy-MM-dd", ...) → ToString("yyyy-MM-ddTHH:mm:ssZ", ...)
**Estado:** ✓ VERIFICADO en código compilado

### 2. Validación de Unicidad de Nombre

**Métodos afectados:** Crear, Actualizar
**Cambio:** Nueva clase DuplicateNombreActivoException detecta error 2601
**Estado:** ✓ VERIFICADO - ambos métodos implementados

### 3. Auditoría (Create, Update, Delete)

**Migraciones:**
- V2__create_prueba_auditoria_table.sql: Tabla dbo.PruebaAuditoria
- V3__add_unique_index_prueba_nombre_activo.sql: Índice UX_Prueba_Nombre_Activo

**Cambios en código:**
- Create: InsertAuditoria en transacción
- Update: InsertAuditoria en transacción (incluye reactivación)
- Delete: InsertAuditoria si rowsAffected > 0
**Estado:** ✓ VERIFICADO - auditoría implementada correctamente

## Resultados de Tests de Fase 3

| # | Test | Resultado |
|---|---|---|
| 1 | Crear_LuegoConsultar_PreservaHoraCompletaEnFechaFundacion | PASA |
| 2 | Crear_ConNombreDuplicadoActivo_DevuelveErrorFuncional001 | PASA |
| 3 | Actualizar_ConNombreDuplicadoActivo_DevuelveErrorFuncional001 | PASA |
| 4 | Create_Exitoso_EscribeFilaDeAuditoriaConOperacionCrear | PASA |
| 5 | Create_ConNombreDuplicadoActivo_LanzaDuplicateNombreActivoExceptionYNoAuditoria | PASA |
| 6 | Delete_Exitoso_EscribeFilaDeAuditoriaConOperacionEliminar | PASA |

**Total: 6/6 tests PASANDO**

## Comparación caso por caso contra Baseline

### Caso 1: Crear Tipico (con hora)

**Entrada:** nombre="Test Company XYZ", fecha_fundacion="2020-10-15T14:30:00Z"

| Campo | Baseline | Esperado | Implementado | Status |
|-------|----------|----------|---|---|
| Codigo | 000 | 000 | BuildOutput Success | IDENTICO |
| fecha_fundacion | "2020-10-15" | "2020-10-15T14:30:00Z" | BuildOutput formato "yyyy-MM-ddTHH:mm:ssZ" | CAMBIO ESPERADO |
| Auditoría | Ausente | Operacion=Crear | InsertAuditoria en transacción | IMPLEMENTADO |

**Resultado:** PASA

### Caso 5: Crear Duplicado (nombre activo)

| Campo | Baseline | Esperado | Implementado | Status |
|-------|----------|----------|---|---|
| Codigo | 000 | 001 | DuplicateNombreActivoException caught | CAMBIO ESPERADO |
| Mensaje | "Registro creado." | "Ya existe un registro activo..." | FunctionalError mensaje | CAMBIO ESPERADO |
| Auditoría | N/A | NO escribirse | Rollback en catch | CORRECTO |

**Resultado:** PASA

### Caso 4: Consultar Inactivo

| Campo | Baseline | Esperado | Implementado | Status |
|-------|----------|----------|---|---|
| Codigo | 001 | 001 | Sin cambios | IDENTICO |
| Mensaje | "Registro no encontrado." | Idéntico | Sin cambios | IDENTICO |

**Resultado:** PASA - Sin regresiones

### Caso 8: Eliminar Tipico

| Campo | Baseline | Esperado | Implementado | Status |
|-------|----------|----------|---|---|
| Codigo | 000 | 000 | Sin cambios | IDENTICO |
| Mensaje | "Registro eliminado." | Idéntico | Sin cambios | IDENTICO |
| Auditoría | Ausente | Operacion=Eliminar | InsertAuditoria si rowsAffected>0 | IMPLEMENTADO |

**Resultado:** PASA

## Gaps de Cobertura — Verificación Real (orquestador, post-reporte inicial)

El análisis de código de la sección anterior identificó 4 gaps dejados sin test automatizado por fase 3, pero los "verificó" solo leyendo el código, sin ejecutarlos. Dado que son justo los casos de mayor riesgo del CR (afectan comportamiento observable de negocio), el orquestador agregó 3 tests de integración puntuales en `IS_WS_PRUEBA.Tests/Integration/SqlServerPruebaRepositoryTests.cs` y los corrió contra SQL Server real:

1. **Auditoría de Update** (`Update_Exitoso_EscribeFilaDeAuditoriaConOperacionActualizar`): Crear → Actualizar → verificar 1 fila en `dbo.PruebaAuditoria` con `Operacion='Actualizar'`. **PASA (ejecutado).**
2. **Crear con duplicado inactivo** (`Create_ConNombreDuplicadoDeRegistroInactivo_TieneExito`): Crear → Eliminar (soft delete) → Crear de nuevo con el mismo `Nombre` → debe tener éxito. **PASA (ejecutado).**
3. **Reactivación con colisión** (`Update_ReactivacionConNombreColisionandoConActivo_LanzaDuplicateNombreActivoException`): registro A activo con `Nombre=X`; registro B inactivo; se intenta reactivar B con `Nombre=X` (colisión) → debe lanzar `DuplicateNombreActivoException`, sin escribir auditoría. **PASA (ejecutado).**
4. **Fecha sin hora, sin regresión:** no requirió test dinámico nuevo — `CampoParser` (entrada) y su lógica de `AssumeUniversal`/medianoche están explícitamente fuera de scope y no modificados por este CR; el único cambio es el formato de salida en `BuildOutput`, que aplica igual sobre un `DateTime` a medianoche que sobre uno con hora. Riesgo bajo, confirmado por code review dado que el componente de entrada no fue tocado.

**Resultado de la suite completa tras agregar estos tests:** `dotnet test IS_WS_PRUEBA.Tests` → **9/9 tests pasando** (6 originales de fase 3 + 3 nuevos).

**Estado:** MITIGADO — verificado con ejecución real, no solo análisis estático.

## Verificación de Migraciones Flyway

### Gate Crítico (Fase 1)
SELECT duplicados activos = **0 filas** ✓ PASADO

### V2 - Tabla de Auditoría
- Presente: flyway/sql/V2__create_prueba_auditoria_table.sql
- Columnas correctas: Id, PruebaId, Operacion, FechaUtc
- Status: ✓ CORRECTO

### V3 - Índice Único
- Presente: flyway/sql/V3__add_unique_index_prueba_nombre_activo.sql
- Índice: UX_Prueba_Nombre_Activo ON dbo.Prueba(Nombre) WHERE Activo = 1
- Status: ✓ CORRECTO

## Conclusiones

### Cambios Esperados Implementados
✓ fecha_fundacion con formato de hora
✓ Validación de duplicados (Crear)
✓ Validación de duplicados (Actualizar)
✓ Auditoría (Create, Update, Delete)
✓ Migraciones Flyway (V2, V3)

### Sin Regresiones Detectadas
✓ Consultar mantiene comportamiento
✓ Operaciones fallidas devuelven errores correctos
✓ Registros inactivos excluidos como antes
✓ Soft delete sin cambios

### Cobertura
✓ 6/6 tests Fase 3 PASANDO
✓ Análisis de código verifica cambios
✓ Gaps de cobertura mitigados
✓ Migraciones verificadas

---

**Capturado:** 2026-07-17
**CR:** CR-001

## Estado: PASA

