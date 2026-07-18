---
description: "Orquesta el pipeline completo de 7 fases para WS_Prueba (behavior-capture -> spec+implementación -> tests unit/integración -> regresión -> validación e2e -> deploy). Corre automáticamente fase a fase; solo se detiene por scope fuera de CR o por error real."
argument-hint: [CR-XXX existente, o descripción en lenguaje natural del change request]
---

Vas a orquestar el pipeline de feature de WS_Prueba para:

"$ARGUMENTS"

Sigue este proceso. A diferencia de un pipeline con aprobación manual entre cada fase, este corre **automáticamente** de principio a fin — la política HITL (ver `CLAUDE.md`) solo te obliga a detenerte en dos casos: (1) necesitas salir del `scope` declarado del CR o hacer algo no contemplado en la spec, o (2) ocurre un error real (test roto, build roto, regresión, deploy fallido). Fuera de esos dos casos, NO pidas confirmación entre fases — continúa solo.

## Paso previo: ¿"$ARGUMENTS" es un CR existente o texto libre?

1. Si "$ARGUMENTS" matchea el patrón `CR-\d+` y existe `change-request/{ARGUMENTS}.md`: ese es el CR a correr, salta directo a la **Fase 0**.
2. Si no existe ese archivo, o "$ARGUMENTS" es una descripción en lenguaje natural: estás creando un CR nuevo.
   - Determina el siguiente número: lista `change-request/CR-*.md` (excluyendo `CR-template.md`), toma el mayor `CR-XXX` existente y usa `XXX+1` con padding de 3 dígitos.
   - Copia la estructura de `change-request/CR-template.md` a `change-request/CR-{nuevo}.md` y llénala con tu mejor lectura del texto recibido:
     - `titulo`: resumen corto.
     - `scope`: explora el código relevante (`Contracts/`, `Domain/`, `Infrastructure/`, `flyway/sql/`) lo suficiente para proponer globs razonables — esto es un punto de partida, el humano lo puede ajustar antes de aprobar.
     - `sistema_afectado`, `Qué`, `Por qué`, `Criterios de aceptación`: usa literalmente lo que te dieron; si algo no es verificable o no se especificó, dilo explícito en vez de inventarlo (ej. en Criterios: "no especificado por el solicitante").
     - `estado: draft`, `creado`: fecha de hoy, `autor`: quien te lo pidió si lo sabes, si no `"sin especificar"`.
     - Deja `estimado_manual_horas` vacío salvo que te lo hayan dado.
   - **NO** escribas `change-request/.active` todavía ni continúes a la Fase 0. Muéstrame el CR creado completo y termina tu turno con:
     `ESPERANDO APROBACIÓN — revisa change-request/CR-{nuevo}.md (especialmente 'scope'), ajústalo si hace falta, cambia 'estado: approved' y vuelve a correr /feature CR-{nuevo}.`
   - Este es el único punto de aprobación obligatorio de todo el pipeline — corresponde a la política HITL punto 1 (nada del scope estaba todavía contemplado/confirmado por un humano).

## Fase 0 — Intake del CR

1. Escribe el id del CR en `change-request/.active` (una sola línea, sin salto final si puedes evitarlo).
2. Lee `change-request/{CR-XXX}.md`. Si `estado` no es exactamente `approved`, DETENTE: reporta qué falta y no continúes (ni siquiera si el texto "se ve bien") — no hay excepción para este gate.
3. Si está `approved`: actualiza `.claude/artifacts/status-pipeline.json` (`cr_id`, `current_phase: "1-behavior-capture"`, fase `0-intake` → `status: "done"` con timestamps) y `.claude/artifacts/blueprint.md` (título real del CR, resumen ejecutivo de 3-5 líneas, fila de fase 0 en verde).
4. Continúa automáticamente a la Fase 1 — no pidas confirmación de la Fase 0.

## Fase 1 — Captura de comportamiento + Spec (en paralelo)

`behavior-capturer` y `spec-writer` NO dependen entre sí — ambos solo necesitan el CR activo (ninguno lee el output del otro). Invócalos en el mismo turno, en paralelo (dos llamadas a Task en un solo mensaje), en vez de esperar a que termine uno para arrancar el otro. Solo `implementer` (Fase 2) necesita el resultado de ambos.

Cuando los dos terminen:
- Lee `specs/CR-XXX/baseline-snapshot.md` (de `behavior-capturer`) y `specs/CR-XXX/spec.md` (de `spec-writer`).
- Si `behavior-capturer` quedó bloqueado (no pudo levantar el entorno o correr un caso): DETENTE, resume el bloqueo y espera confirmación antes de reintentar o ajustar.
- Si `spec-writer` dejó "Preguntas abiertas" que bloquean poder implementar con confianza, o si el alcance propuesto no cabe en el `scope` del CR: DETENTE (política HITL punto 1) y pide la aclaración/ajuste de scope antes de seguir.
- Si ambos terminaron bien: actualiza `blueprint.md`/`status-pipeline.json` (fase 1 → `done`, fase activa → `2-implementacion`) y continúa solo a la Fase 2.

## Fase 2 — Implementación (implementer)

1. Invoca `implementer` con el spec + baseline ya listos. El hook `scope-guard` bloqueará cualquier intento de tocar algo fuera de `scope:` — si eso pasa, no lo rodees ni cambies el scope tú mismo; DETENTE y pide la decisión humana.
2. Si `implementer` termina con el build local en verde: actualiza `blueprint.md`/`status-pipeline.json` (fase 2 → `done`, fase activa → `3-tests-unitarios-integracion`) y continúa solo a la Fase 3. La corrida formal y autoritativa de `dotnet test` es la de `unit-integration-tester` en la Fase 3 — `implementer` ya no la duplica (ver su propio prompt).

## Fase 3 — Tests unitarios/integración (gate)

Invoca `unit-integration-tester`. Lee `specs/CR-XXX/unit-integration-report.md`:
- `Estado: FALLA` → DETENTE (política HITL punto 2), resume qué test/build falló y espera antes de reintentar.
- `Estado: PASA` → actualiza artefactos (fase 3 → `done`, fase activa → `4-tests-regresion`) y continúa solo a la Fase 4.

## Fase 4 — Tests de regresión (gate)

Invoca `regression-tester`. Lee `specs/CR-XXX/regression-report.md`:
- `Estado: FALLA` (alguna regresión no justificada por la spec, o migración Flyway no idempotente) → DETENTE (política HITL punto 2).
- `Estado: PASA` → actualiza artefactos (fase 4 → `done`, fase activa → `5-validacion-e2e`) y continúa solo a la Fase 5.

## Fase 5 — Validación end-to-end

Invoca `validator`. Lee `specs/CR-XXX/validation-report.md`:
- `Estado: RECHAZADO` → DETENTE (política HITL punto 2).
- `Estado: APROBADO` → actualiza artefactos (fase 5 → `done`, fase activa → `6-deploy`) y continúa solo a la Fase 6.

## Fase 6 — Deploy (infraestructura de datos)

Invoca `deployer`. Lee `specs/CR-XXX/deploy-report.md`:
- `Estado: RECHAZADO` → DETENTE (política HITL punto 2).
- `Estado: APROBADO`:
  1. Cambia `estado: approved` → `estado: done` en `change-request/CR-XXX.md`.
  2. Vacía `change-request/.active` (ya no hay CR corriendo).
  3. Si `estimado_manual_horas` no viene en el frontmatter del CR, pídemelo explícitamente antes de cerrar — no inventes el número. Con ese dato, consolida la tabla de ROI en `.claude/artifacts/blueprint.md` (horas de análisis/spec, implementación, testing, validación+deploy — estimado manual vs. real con pipeline, usando los timestamps `started_at`/`completed_at` de `status-pipeline.json` para el "real").
  4. Marca fase 6 → `done` en `status-pipeline.json` y deja `current_phase` en `"6-deploy"` (el pipeline para este CR ya cerró).
  5. Recuérdame que el arranque de la app (`IS_WS_PRUEBA.asmx` vía IIS Express/Visual Studio) y el `git commit`/`push` del código siguen siendo pasos manuales — este pipeline no los automatiza.

## Regla general de comunicación

Cada vez que te detengas por la política HITL, tu último mensaje debe decir explícitamente:
`ESPERANDO APROBACIÓN — <qué pasó: scope-violation | test-failure | build-failure | regresión | deploy-fallido> en fase <N> (<nombre>)`
y registrar el evento en `hitl_events` de `status-pipeline.json` (`phase`, `reason`, `timestamp`, `resolution` en blanco hasta que yo responda).

Cuando una fase termina bien, NO pidas confirmación — dilo brevemente y sigue a la siguiente fase en el mismo turno.
