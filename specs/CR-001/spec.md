# Spec — CR-001: Auditoría de cambios, restricción de unicidad en nombre y fix de truncado de hora en fecha_fundacion

## CR de origen
`change-request/CR-001.md` (estado: approved, 2026-07-17)

**Qué:** tres cambios sobre la capa de persistencia SQL Server de `IS_WS_PRUEBA`:
1. Auditoría de `Crear`/`Actualizar`/`Eliminar` (incluida reactivación vía `Actualizar` con `activo=true`) en tabla nueva `dbo.PruebaAuditoria`, solo almacenamiento (sin operación SOAP consultable).
2. Índice único filtrado `UX_Prueba_Nombre_Activo` sobre `dbo.Prueba(Nombre) WHERE Activo = 1`; violación traducida a `codigo=001`.
3. Fix de truncado de hora en `fecha_fundacion`: causa raíz confirmada en `PruebaCrudService.BuildOutput` (línea ~264), un simple cambio de formato de salida.

**Por qué:** trazabilidad de cambios para soporte/compliance; integridad real de `Nombre` a nivel de BD (no solo capa de aplicación); consistencia entre lo que el contrato de entrada acepta (fecha+hora) y lo que la respuesta SOAP devuelve.

**Criterios de aceptación:** ver sección "Criterios de aceptación" abajo (copiados del CR, agrupados por sub-cambio).

## Alcance

**Sí incluye:**
- Migración(es) Flyway nuevas: tabla `dbo.PruebaAuditoria` + índice único filtrado `UX_Prueba_Nombre_Activo`.
- Escritura de auditoría dentro de `SqlServerPruebaRepository.Create/Update/Delete`, en la misma transacción SQL que la operación principal.
- Traducción de la violación del índice único a `codigo=001` en `PruebaCrudService.Crear/Actualizar`.
- Cambio de una línea de formato en `PruebaCrudService.BuildOutput` para `fecha_fundacion`.
- Tests xUnit nuevos en `IS_WS_PRUEBA.Tests` (proyecto a crear, no existe hoy) y su registro en `IS_WS_PRUEBA.sln`.

**No incluye (explícito, para evitar scope creep):**
- Ninguna operación SOAP nueva de consulta de auditoría.
- Usuario/origen/IP en auditoría — solo operación, id, timestamp.
- Normalización de `Nombre` (mayúsculas/acentos/espacios) para la comparación de unicidad.
- Cambios a `CampoParser.AllowedDateFormats` ni a `DateTimeStyles.AssumeUniversal`.
- Cambios a `Contracts/` (ningún DTO cambia de forma) ni versionado del contrato SOAP.
- Cambios a `Domain/Entities/PruebaRecord.cs` — no hace falta una entidad de dominio para auditoría; la escritura vive enteramente en `Infrastructure`.
- Cambios a `Infrastructure/IPruebaRepository.cs` ni `Infrastructure/InMemoryPruebaRepository.cs` — no forman parte de `scope:` del CR y no son necesarios (ver "Componentes afectados").
- Cualquier FK, índice adicional o `CHECK constraint` sobre `dbo.PruebaAuditoria` no listado explícitamente en la tabla "Contrato de datos" del CR.
- `Web.config`, autenticación, u otros hallazgos de seguridad — CR separado.

**Confirmación de alcance vs. `scope:` del CR:**
```
scope:
  - "flyway/sql/**"                              → 2 migraciones nuevas (ver abajo)
  - "Domain/Services/PruebaCrudService.cs"        → catch de violación única + fix BuildOutput
  - "Domain/Services/CampoParser.cs"              → sin cambios funcionales (incluido en scope pero el CR confirma que no se toca)
  - "Infrastructure/SqlServerPruebaRepository.cs" → transacción + auditoría + excepción identificable
  - "IS_WS_PRUEBA.Tests/**"                       → proyecto nuevo (xUnit, net48) con tests unitarios + integración
  - "IS_WS_PRUEBA.sln"                            → registrar el nuevo proyecto de tests
```
Todo lo propuesto en este spec cabe dentro de estos globs. No se toca `Contracts/`, `Domain/Entities/`, `Infrastructure/IPruebaRepository.cs`, `Infrastructure/InMemoryPruebaRepository.cs`, `IS_WS_PRUEBA.asmx.cs` ni `Web.config`.

## Componentes afectados

### Flyway (`flyway/sql/`)
Dos migraciones nuevas (separadas, siguiendo el patrón atómico de `V1`), numeradas a continuación de `V1__create_prueba_table.sql`:

- **`V2__create_prueba_auditoria_table.sql`**
  - `CREATE TABLE dbo.PruebaAuditoria (Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PruebaAuditoria PRIMARY KEY, PruebaId INT NOT NULL, Operacion VARCHAR(20) NOT NULL, FechaUtc DATETIME2 NOT NULL);`
  - Literal según el "Contrato de datos" del CR — **no** agregar FK a `dbo.Prueba(Id)`, **no** agregar `CHECK` sobre `Operacion`, **no** agregar índices adicionales ni `DEFAULT` en `FechaUtc` (el valor siempre se provee explícitamente desde `SqlServerPruebaRepository`, igual que `FechaActualizacion` en `Create`/`Update` de `dbo.Prueba`). Si se cree necesario algo adicional, es una ampliación de scope — no implementarla, anotarla como sugerencia.
- **`V3__add_unique_index_prueba_nombre_activo.sql`**
  - `CREATE UNIQUE INDEX UX_Prueba_Nombre_Activo ON dbo.Prueba (Nombre) WHERE Activo = 1;`
  - Precondición (responsabilidad de `behavior-capturer` en fase 1, no de esta migración): confirmar que no existen hoy duplicados activos de `Nombre` (`SELECT Nombre, COUNT(*) FROM dbo.Prueba WHERE Activo=1 GROUP BY Nombre HAVING COUNT(*)>1`). Si los hay, el pipeline se detiene antes de llegar a fase 2 — esta migración no debe aplicarse hasta que eso se resuelva.
  - Ninguna migración altera ni recalcula datos existentes (confirmado por el CR).

### Domain/Services (`PruebaCrudService.cs`)
- **`BuildOutput`** (línea ~264): cambiar
  `record.FechaFundacion.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)`
  a
  `record.FechaFundacion.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)`.
  Único cambio en este método. No tocar `fecha_actualizacion` (ya usa ese formato) ni ningún otro campo.
- **`Crear`** y **`Actualizar`**: agregar un `catch` específico, **antes** del `catch (Exception exception)` genérico existente, para la excepción identificable que lance `SqlServerPruebaRepository` ante violación del índice único (ver diseño en Infrastructure abajo), devolviendo `FunctionalError("Ya existe un registro activo con ese nombre.")` (codigo=001). El `catch` genérico sigue existiendo para cualquier otro error → codigo=900, sin cambios.
- **No se requiere ningún cambio adicional en `Crear`/`Actualizar`/`Eliminar` para la escritura de auditoría.** El texto del CR ("vía el repositorio, no acoplado directamente a SQL desde el dominio") junto con el requisito de "misma transacción SQL" implican que la escritura de auditoría vive **enteramente dentro** de `SqlServerPruebaRepository.Create/Update/Delete` — invisible para `PruebaCrudService`, que sigue llamando a `_repository.Create(...)`, `_repository.Update(...)`, `_repository.Delete(id)` exactamente igual que hoy. Si `implementer` considera necesario que `PruebaCrudService` llame a un método de auditoría separado, **debe detenerse y preguntar**, porque eso rompería la garantía de "misma transacción" (una segunda llamada al repositorio abriría una conexión/transacción distinta).

### Infrastructure (`SqlServerPruebaRepository.cs`)
- **`Create`**: envolver el `INSERT ... OUTPUT INSERTED.*` existente y un nuevo `INSERT INTO dbo.PruebaAuditoria (PruebaId, Operacion, FechaUtc) VALUES (@PruebaId, 'Crear', SYSUTCDATETIME());` en una única `SqlTransaction` sobre la misma `SqlConnection`. `@PruebaId` = el `Id` devuelto por el `OUTPUT` del INSERT principal. Commit solo si ambos statements tienen éxito; si el segundo falla, rollback (revierte también el registro principal).
- **`Update`**: mismo patrón — `UPDATE ... OUTPUT INSERTED.*` + `INSERT INTO dbo.PruebaAuditoria (..., 'Actualizar', ...)` en la misma transacción. Se ejecuta con `Operacion='Actualizar'` siempre, sin distinguir si es una edición normal o una reactivación (`Activo` pasando de 0 a 1) — no hay branching de código para ese caso, tal como confirma el CR.
- **`Delete`**: cambiar de `command.ExecuteNonQuery() > 0` directo a: `UPDATE ... SET Activo=0 ...` dentro de una transacción; solo si `ExecuteNonQuery() > 0` (el `Id` existía), insertar la fila de auditoría (`Operacion='Eliminar'`) y hacer commit; si `ExecuteNonQuery()` devuelve 0, no hay nada que auditar — rollback/no-op y `return false`.
- **Excepción identificable para violación de unicidad**: definir una clase pública nueva **dentro de este mismo archivo** (`SqlServerPruebaRepository.cs`, no un archivo nuevo — evita tocar rutas fuera de `scope:`), p. ej.:
  ```csharp
  public sealed class DuplicateNombreActivoException : Exception
  {
      public DuplicateNombreActivoException(Exception inner)
          : base("Ya existe un registro activo con ese nombre.", inner) { }
  }
  ```
  En `Create`/`Update`, capturar `SqlException`, verificar si corresponde a la violación de `UX_Prueba_Nombre_Activo`, y si es así, hacer rollback y lanzar `DuplicateNombreActivoException`; si no, hacer rollback y relanzar la excepción original (para que `PruebaCrudService` la trate como error técnico `900`, comportamiento sin cambios).
  - **Detección de la violación**: dado que el índice se crea como `CREATE UNIQUE INDEX` (no como `CONSTRAINT` con nombre propio vía `ALTER TABLE`), el número de error esperado en SQL Server es **2601** ("Cannot insert duplicate key row ... with unique index ..."), no 2627 (que aplica a `CONSTRAINT`s nombrados vía `ALTER TABLE ADD CONSTRAINT`). Verificar `sqlException.Number == 2601` **y además** que `sqlException.Message` contenga el literal `"UX_Prueba_Nombre_Activo"`, para no clasificar erróneamente una futura violación de otro índice único distinto como si fuera esta. `unit-integration-tester` (fase 3) debe confirmar empíricamente el número de error contra SQL Server 2022 real; si difiere de 2601, es un ajuste puntual dentro de este mismo diseño, no una ambigüedad de negocio.
- El mapeo de `fecha_fundacion` (`SqlDbType.DateTime2` en `Create`/`Update`, `reader.GetDateTime(3)` en `Map`) **no se modifica** — confirmado por el CR que ya preserva la hora completa.

### Contracts
Sin cambios. `listaCampos`/`listaCamposSalida` no cambian de forma (confirmado por criterio de aceptación explícito).

### Migración Flyway requerida
**Sí** — dos archivos nuevos:
- `flyway/sql/V2__create_prueba_auditoria_table.sql`
- `flyway/sql/V3__add_unique_index_prueba_nombre_activo.sql`

## Criterios de aceptación
*(copiados del CR, sin inventar nuevos)*

**Auditoría**
- [ ] `Crear` exitoso → existe fila en `dbo.PruebaAuditoria` con `Operacion='Crear'`, `PruebaId` correcto, `FechaUtc` en UTC.
- [ ] `Actualizar` exitoso (incluida reactivación con `activo=true`) → existe la fila de auditoría correspondiente (`Operacion='Actualizar'`).
- [ ] `Eliminar` exitoso → existe la fila de auditoría correspondiente (`Operacion='Eliminar'`).
- [ ] Operación fallida (`codigo=001` o `900`) → **no** se registra fila de auditoría.
- [ ] `listaCampos`/`listaCamposSalida` no cambian de forma tras agregar auditoría.

**Unicidad de nombre**
- [ ] `Crear` con `Nombre` igual a un registro activo existente → `codigo=001`, mensaje "Ya existe un registro activo con ese nombre.", no crea el registro.
- [ ] `Actualizar` que intenta poner un `Nombre` que colisiona con otro registro activo → `codigo=001`, no aplica el cambio.
- [ ] `Crear` con `Nombre` igual a un registro **inactivo** → éxito.
- [ ] Reactivación (`Actualizar` con `activo=true`) cuyo `Nombre` colisiona con otro activo → `codigo=001`.

**Fix de fecha_fundacion**
- [ ] `Crear` con `fecha_fundacion="2020-10-15T14:30:00Z"` → `Consultar` conserva `14:30:00`.
- [ ] Mismo caso, inspección directa en SQL Server → columna almacena hora completa (ya lo hace hoy, no cambia).
- [ ] `Crear` con `fecha_fundacion="2020-10-15"` (sin hora) → se comporta igual que hoy (`00:00:00Z`), sin regresión.
- [ ] El cambio se limita a la línea de formateo en `BuildOutput` — no se tocan `CampoParser` ni `SqlServerPruebaRepository` para este fix.

## Riesgos identificados
- La restricción de unicidad puede rechazar `Crear`/`Actualizar` que hoy tienen éxito con `Nombre` duplicado — cambio de comportamiento intencional ya aprobado por el CR, pero con consumidores SOAP potencialmente afectados (conocidos o no).
- El fix de `fecha_fundacion` cambia un valor de salida ya observable (`Consultar` dejará de devolver `00:00:00` cuando el cliente envió hora) — mismo riesgo de consumidores existentes, ya aceptado en el CR.
- `InMemoryPruebaRepository` (fuera de `scope:`) **no** implementa auditoría ni validación de unicidad — cualquier test que use ese repositorio para estos escenarios dará falsos negativos; los tests de auditoría/unicidad deben ejercitar `SqlServerPruebaRepository` contra SQL Server real (vía `docker compose up -d`).
- Precondición externa a este spec: el chequeo de duplicados preexistentes de `Nombre` (`behavior-capturer`, fase 1) debe completarse y no encontrar filas antes de que `V3` se aplique; si `V3` se aplicara con duplicados activos existentes, `CREATE UNIQUE INDEX` fallaría al aplicar la migración (falla segura, no corrupción silenciosa, pero bloquea el pipeline igual).
- Nota informacional (no bloqueante): el seed de `flyway/sql/V1__create_prueba_table.sql` incluye un comentario ("Registro duplicado a propósito en Nombre...") sobre `Prueba Theta`, pero los valores de `Nombre` del seed actual no tienen duplicados reales entre sí — el comentario no refleja el dato real. No afecta este CR, pero puede ser relevante para que `behavior-capturer` no asuma que el chequeo de duplicados va a encontrar algo por defecto.
- Número de error SQL Server (2601) asumido para violación de índice único filtrado creado vía `CREATE UNIQUE INDEX` — diseño técnico razonado, pero su validación empírica final ocurre en fase 3 (`unit-integration-tester`) contra SQL Server real.

## Preguntas abiertas
Ninguna. El CR resolvió explícitamente los 6 puntos abiertos originales (naming, auditoría de reactivación, transaccionalidad, tipo de columna `fecha_fundacion`, umbral de latencia, manejo de duplicados preexistentes) y la tabla "Contrato de datos" es suficiente para implementar sin inventar nombres ni reglas de negocio. Las decisiones de diseño técnico no cubiertas literalmente por el CR (nombre/ubicación de la excepción identificable, número de archivos de migración, número de error SQL a verificar) se resolvieron en este spec dentro del `scope:` declarado, sin alterar el alcance del CR ni las reglas de negocio.
