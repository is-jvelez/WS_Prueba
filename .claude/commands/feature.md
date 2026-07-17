---
description: "Ejecuta el pipeline completo de un feature sobre WS_Prueba (spec-driven, 5 fases, con aprobación humana entre cada una)"
argument-hint: [descripción del change request]
---

Vas a coordinar el pipeline de feature para WS_Prueba sobre el siguiente change request:

"$ARGUMENTS"

Sigue este proceso EXACTO, sin saltarte fases ni fusionarlas.

## 0. Setup
- Determina el siguiente número de CR revisando las carpetas existentes en `specs/CR-*`.
- Crea `specs/CR-XXX/` (usa el número siguiente, con padding de 3 dígitos).

## 1. Spec
- Invoca al subagente `spec-writer` pasándole el change request completo.
- Guarda su output en `specs/CR-XXX/spec.md`.
- Muéstrame un resumen del spec y DETENTE. No continúes hasta que yo escriba "aprobado" o pida cambios.

## 2. Behavior capture
- Solo después de mi aprobación del spec: invoca a `behavior-capturer` pasándole `specs/CR-XXX/spec.md`.
- Guarda su output en `specs/CR-XXX/baseline-snapshot.md`.
- Muéstrame qué comportamiento capturó y DETENTE. Espera mi aprobación explícita.

## 3. Implementación
- Solo después de mi aprobación del baseline: invoca a `implementer` pasándole el spec y el baseline.
- No apruebes tú mismo el resultado — muéstrame el resumen de cambios (diff) y DETENTE.

## 4. Validación y regresión
- Solo después de que yo confirme que el código se ve bien: invoca a `validator` pasándole spec, baseline y los cambios.
- Guarda su output en `specs/CR-XXX/validation-report.md`.
- Muéstrame el resultado (pass/fail, comparación contra baseline) y DETENTE.

## 5. Release
- Solo si `validation-report.md` dice "Estado: APROBADO" Y yo doy el OK explícito para desplegar: invoca a `release`.
- El hook de PreToolUse ya bloqueará cualquier `git push` si el validation-report no está aprobado — no intentes rodear ese control ni asumas que puedes saltártelo. Ese hook NO bloquea `docker compose up/down` (es infraestructura local de dev/test, siempre reversible); la única barrera real para el deploy/push es la aprobación humana explícita de este paso.

## Regla general
En cada fase, tu último mensaje antes de detenerte debe decir explícitamente:
`ESPERANDO APROBACIÓN — fase X de 5 (<nombre de la fase>)`
para que quede claro que no vas a continuar sin que yo lo confirme.