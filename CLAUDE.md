# WS_Prueba — contexto para Claude Code

## Qué es este proyecto

Web service legacy **.NET Framework 4.8** (SOAP/WebForms clásico, `IS_WS_PRUEBA.asmx` + `IS_WS_PRUEBA.asmx.cs`), con arquitectura hexagonal parcial:

- `Contracts/` — DTOs SOAP (`SoapRequestDto`, `SoapCampoDto`, `SoapResponseDto`, `ServiceContractMetadata`). El contrato es genérico: todas las operaciones reciben/devuelven una lista de pares `{name, value, type}` (`listaCampos` / `listaCamposSalida`), no parámetros tipados por operación.
- `Domain/Entities/` — entidades de negocio (`PruebaRecord`).
- `Domain/Services/` — lógica de negocio (`PruebaCrudService` orquesta las 4 operaciones CRUD; `CampoParser` valida/parsea `listaCampos` hacia tipos fuertes).
- `Infrastructure/` — repositorios: `IPruebaRepository` (puerto), `SqlServerPruebaRepository` (ADO.NET puro contra SQL Server, sin ORM), `InMemoryPruebaRepository` (para tests).
- `flyway/` — migraciones versionadas de base de datos (`flyway/sql/V{n}__descripcion.sql`), aplicadas por el servicio `flyway` de `docker-compose.yml`.
- `IS_WS_PRUEBA.Tests` — proyecto de tests (unit + integración). **Hoy está vacío** (sin `.csproj`, no registrado en `IS_WS_PRUEBA.sln`); el pipeline usa **xUnit** como framework acordado (SDK-style, target `net48`, corrible con `dotnet test`) y lo crea la primera vez que un CR necesita tests.

### Cómo se levanta el entorno

`docker-compose.yml` levanta **únicamente la capa de datos**: SQL Server 2022 (contenedor `is_ws_prueba_sqlserver`, puerto host `${SQLSERVER_HOST_PORT:-15533}` porque 1433/14330 ya están ocupados en esta máquina) + un servicio `db-init` que crea la base si no existe + el servicio `flyway` que migra `flyway/sql/*.sql` al arrancar. La app **no corre en contenedor**: se levanta local vía IIS Express/Visual Studio, apuntando a `localhost,15533` (ver cadena de conexión `IS_WS_PRUEBAConnection` en `Web.config`).

```bash
docker compose up -d          # levanta SQL Server + aplica migraciones Flyway
docker compose down -v        # baja el entorno y borra el volumen (para pruebas limpias)
dotnet build                  # compila IS_WS_PRUEBA (net48)
dotnet test IS_WS_PRUEBA.Tests
```

`.env` (no versionado; copiar de `.env.example`) define `SA_PASSWORD` y `DB_NAME` para docker-compose. Deben coincidir con la cadena de conexión de `Web.config`.

## Convenciones detectadas en el código real

- **Códigos de respuesta SOAP**: `Codigo = "000"` éxito, `"001"` error funcional/validación (mensaje de negocio, ej. "Registro no encontrado."), `"900"` error técnico inesperado (siempre con mensaje genérico, el detalle real va a `Trace.TraceError`, nunca al cliente SOAP).
- **Nombres de campos en `listaCampos`/`listaCamposSalida`**: `snake_case` (`fecha_fundacion`, `fecha_actualizacion`), no `camelCase`/`PascalCase`.
- **Borrado lógico, no físico**: `Eliminar` hace `UPDATE ... SET Activo = 0`, nunca `DELETE`. `Consultar`/`Actualizar` tratan un registro con `Activo = 0` como "no encontrado".
- **Acceso a datos**: ADO.NET puro (`SqlConnection`/`SqlCommand` parametrizados), sin ORM. Patrón `OUTPUT INSERTED.*` en `Create`/`Update` para devolver el registro ya persistido en la misma ida a BD.
- **Migraciones Flyway**: versionadas y nunca modificadas una vez aplicadas; nombre `V{n}__descripcion_en_snake_case.sql` en `flyway/sql/`.
- **Manejo de errores**: cada método público de `PruebaCrudService` envuelve su lógica en `try/catch`; excepciones no controladas nunca llegan al SOAP response tal cual (se traducen a error técnico `"900"`).

## Regla dura: todo cambio de código pasa por un CR activo

Cualquier cambio de código en `Domain/`, `Infrastructure/` o `Contracts/` **DEBE originarse desde un CR aprobado en `change-request/`**, ejecutado con el comando `/feature`, que corre las 7 fases del pipeline (ver abajo) invocando a los subagentes en `.claude/agents/`.

**Nunca edites `Domain/`, `Infrastructure/` o `Contracts/` fuera del scope declarado en el CR activo sin aprobación humana explícita.** El hook `scope-guard` (`PreToolUse` sobre `Edit`/`Write`) bloquea automáticamente cualquier edición fuera de los globs declarados en `scope:` del CR activo — esto no es una sugerencia, es un gate técnico. No saltes fases del pipeline aunque el cambio parezca trivial: `behavior-capturer` existe precisamente para los cambios que "parecen triviales" en un sistema legacy sin cobertura de tests previa.

## El pipeline — 7 fases

Orquestado por `/feature` (`.claude/commands/feature.md`). Cada fase invoca un subagente de `.claude/agents/` y actualiza `.claude/artifacts/blueprint.md` + `.claude/artifacts/status-pipeline.json` al terminar.

| # | Fase | Agente | Qué hace | Artefacto |
|---|---|---|---|---|
| 0 | Intake del CR | orquestador | Lee `change-request/.active`, valida que el CR exista y esté `estado: approved` | — |
| 1 | Captura de comportamiento + Spec | `behavior-capturer` + `spec-writer` (en paralelo — ninguno depende del otro) | `behavior-capturer` documenta el comportamiento real actual (golden master); `spec-writer` convierte el CR en spec técnica. Ambos solo necesitan el CR activo | `specs/CR-XXX/baseline-snapshot.md`, `specs/CR-XXX/fixtures/`, `specs/CR-XXX/spec.md` |
| 2 | Implementación | `implementer` | Implementa dentro del `scope` declarado, usando el spec + baseline de la fase 1 | código + migración Flyway + tests |
| 3 | Tests unitarios/integración | `unit-integration-tester` | Corre `dotnet test IS_WS_PRUEBA.Tests` (unit + integración contra SQL Server real) — única corrida autoritativa de tests del pipeline. **Gate**: no avanza si falla | `specs/CR-XXX/unit-integration-report.md` |
| 4 | Tests de regresión | `regression-tester` | Re-ejecuta los fixtures del baseline (fase 1) contra el código nuevo y compara respuesta por respuesta. **Gate**: no avanza si hay diffs no esperados por la spec | `specs/CR-XXX/regression-report.md` |
| 5 | Validación end-to-end | `validator` | Smoke test. Si el CR no requirió migración Flyway nueva, reusa el entorno (`docker compose up -d`, sin volumen); si sí, reset completo (`down -v && up -d`) para probar la migración desde cero | `specs/CR-XXX/validation-report.md` |
| 6 | Deploy | `deployer` | Levanta/actualiza **solo la infraestructura de datos** vía `docker compose` (build/up/healthcheck) y aplica migraciones Flyway pendientes. **No** despliega la app .asmx — eso sigue siendo local vía IIS Express/Visual Studio; el deployer lo deja explícito como paso manual | `specs/CR-XXX/deploy-report.md` |

Al final de la fase 6, el orquestador cierra el CR (`estado: done`) y consolida el ROI (estimado manual vs. real con pipeline) en `.claude/artifacts/blueprint.md`.

**Decisiones para que el pipeline no tarde de más:** `spec-writer` e `implementer` usan `model: sonnet` (necesitan juicio sobre ambigüedad/código); los demás agentes (`behavior-capturer`, `unit-integration-tester`, `regression-tester`, `validator`, `deployer`) usan `model: haiku` porque su trabajo es ejecutar comandos fijos y llenar un reporte con formato exacto, no razonar sobre diseño. Ningún agente corre `dotnet test` dos veces (solo `unit-integration-tester` en fase 3; `implementer` solo hace `dotnet build` como chequeo rápido). `validator` solo paga el costo de un reset completo de Docker (`down -v`, el paso más lento del pipeline por el arranque frío de SQL Server) cuando el CR realmente agregó una migración Flyway nueva.

## Política HITL (obligatoria, sin excepciones)

El pipeline corre de fase en fase **sin pedir aprobación**, con únicamente estas dos excepciones:

1. **Antes de aplicar un ajuste** que implique salir del scope declarado en el CR activo, o que no estaba contemplado en la spec original.
2. **Cuando ocurre un error** (test fallido, build roto, deploy fallido, comportamiento distinto al golden master).

En ambos casos el agente/orquestador se detiene, resume claramente qué pasó y qué opciones hay, y espera confirmación explícita antes de continuar. En cualquier otro escenario (fase completada exitosamente, tests en verde, deploy sano) el pipeline **continúa automáticamente** a la siguiente fase sin preguntar.

## Dónde viven los artefactos de cada change request

- `change-request/{CR-XXX}.md` — el CR mismo (frontmatter + qué/por qué/criterios/scope). No se edita directamente el `estado` fuera del pipeline.
- `change-request/.active` — el CR en curso ahora mismo (una sola línea, ej. `CR-001`). Vacío si no hay ningún CR corriendo por el pipeline.
- `specs/CR-XXX/spec.md`, `baseline-snapshot.md`, `fixtures/`, `unit-integration-report.md`, `regression-report.md`, `validation-report.md`, `deploy-report.md` — los genera el pipeline, no se crean a mano.
- `.claude/artifacts/blueprint.md` y `.claude/artifacts/status-pipeline.json` — estado vivo del CR en curso, actualizado por el orquestador después de cada fase.

`blueprint.md` en la raíz del repo es un documento distinto: el blueprint de arquitectura del sistema completo (no de un CR puntual) — no lo confundas con `.claude/artifacts/blueprint.md`.
