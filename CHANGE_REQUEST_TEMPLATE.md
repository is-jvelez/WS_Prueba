# Plantilla de solicitud de cambio — IS_WS_PRUEBA

> Copia este archivo (o su contenido) al pedir un cambio nuevo sobre este servicio.
> Entre más completes las secciones, menos idas y vueltas necesita la IA para entender
> el alcance real. Las secciones marcadas **(obligatorio)** son las que más evitan
> retrabajo; el resto ayuda pero se puede omitir si no aplica.

## 0. Identificación

- **CR / Ticket:** CR-XXX (usa el mismo número en el nombre de rama, ej. `feature/prueba-CR-XXX-1`)
- **Fecha de solicitud:**
- **Solicitante:**

## 1. Contexto y motivación **(obligatorio)**

¿Por qué se necesita este cambio? ¿Qué lo dispara — un incidente, un requerimiento de
negocio/compliance, una migración previa, un bug reportado? Sin esto, la IA no puede
priorizar bien entre varias formas válidas de resolver lo mismo.

## 2. Objetivo **(obligatorio)**

Una o dos frases: qué debe ser distinto después del cambio, en términos de comportamiento
observable (no de implementación).

## 3. Alcance

### Dentro de alcance **(obligatorio si hay ambigüedad)**
- ...

### Fuera de alcance **(obligatorio si hay ambigüedad)**
- ... (todo lo que alguien podría asumir que está incluido pero no lo está)

> Si el cambio toca una operación (Crear/Consultar/Actualizar/Eliminar/otra) que **no
> existe todavía** en el código, dilo explícitamente — no asumas que la IA debe crearla.

## 4. Si es un bug: diagnóstico esperado antes del fix

- Reproducir el síntoma con datos concretos (no solo describirlo).
- Rastrear el dato/comportamiento por cada capa relevante antes de tocar código:
  petición → parseo/validación → dominio → persistencia (columna SQL) → lectura →
  formateo de salida. El fix va donde está la causa raíz, no donde es más fácil parchar.
- Confirmar qué capas **ya funcionan bien** para no tocarlas de más.

## 5. Restricciones que no se deben romper **(obligatorio)**

Marca las que apliquen (son las más comunes en este servicio):
- [ ] No cambiar el contrato SOAP existente (WebMethods, `SoapRequestDto`/`SoapResponseDto`,
      nombres/tipos de `campo`) salvo que se pida explícitamente.
- [ ] No degradar tiempo de respuesta de forma perceptible.
- [ ] Todo cambio de esquema va en una migración Flyway nueva (`flyway/sql/V{n}__*.sql`),
      nunca editando una migración ya aplicada.
- [ ] Mantener el patrón de códigos de respuesta: `000` éxito, `001` error funcional
      (validación/negocio — mensaje claro para el consumidor), `900` error técnico
      (catch-all, sin detalle interno expuesto).
- [ ] No romper datos existentes (seed u otros registros ya persistidos).
- [ ] Otra: ...

## 6. Criterios de aceptación **(obligatorio)**

Lista concreta de comportamientos esperados, en términos de qué `codigo`/`mensaje`/campos
de salida debe devolver cada escenario relevante (éxito, error funcional, edge cases).

## 7. Entorno de pruebas — checklist para evitar falsos negativos

> Ya nos pasó: existe más de una copia de este proyecto en la máquina, cada una con su
> propio sitio de IIS Express. Antes de reportar "esto no funcionó", verifica:

- [ ] La carpeta que se editó es la misma que sirve el sitio contra el que se prueba
      (confirmar con el PID/`CommandLine` de `iisexpress.exe` o el puerto real usado).
- [ ] El contenedor Docker de SQL Server está corriendo y sano
      (`docker compose ps` / healthcheck).
- [ ] Si se agregaron migraciones nuevas, se reconstruyó la imagen de Flyway antes de
      migrar (`docker compose build flyway`) — el Dockerfile copia `sql/` al build, así
      que una imagen vieja no ve migraciones nuevas.
- [ ] Se ejecutó `flyway migrate` y se confirmó la versión de esquema resultante.
- [ ] Se compiló el proyecto (MSBuild) después de cambios en `.cs` antes de probar.

## 8. Verificación esperada al cerrar el cambio **(obligatorio)**

Cómo se va a confirmar que funciona, no solo que compila:
- Migraciones aplicadas limpiamente (sin conflicto con datos existentes).
- Build sin errores.
- Prueba funcional de cada escenario del criterio de aceptación (SOAP real o llamada
  directa al servicio), incluyendo al menos un caso de regresión sobre datos ya existentes.

## 9. Post-implementación: registrar en `blueprint.md` **(obligatorio)**

Al cerrar el cambio, agregar una entrada nueva en `blueprint.md` (sección
"Registro de cambios", la más reciente arriba) con:

1. **Resumen ejecutivo** — qué cambió, por qué, y el resultado en 3-5 líneas, en
   lenguaje que entienda alguien que no vio la conversación.
2. **Alcance real** (dentro/fuera) tal como quedó implementado — puede diferir un poco
   de lo pedido si surgió una decisión de diseño durante el trabajo; que quede explícita.
3. **Archivos modificados** (lista corta de rutas, no diffs completos).
4. **Sección de ROI** con esta tabla:

   | | |
   |---|---|
   | Tiempo con IA (esta sesión) | ~X min/horas |
   | Tiempo manual estimado (dev senior, sin IA) | ~X-Y horas |
   | Ahorro estimado | ~XX% |

   - El "tiempo con IA" es el tiempo real de sesión (de la solicitud a la verificación
     cerrada) — si lo puedes anotar tú (hora de inicio/fin), es exacto; si no, la IA lo
     estima a partir de la actividad observable en la conversación y lo marca como
     estimación.
   - El "tiempo manual estimado" es una estimación razonada (no una medición real) de
     cuánto tardaría un desarrollador senior sin asistencia de IA en el mismo cambio,
     incluyendo diagnóstico, implementación y pruebas — debe justificarse brevemente
     (qué partes son las que más tiempo tomarían manualmente y por qué).
   - Si con el tiempo se empieza a medir el tiempo manual real (por haberlo hecho antes
     con y sin IA), reemplazar la estimación por el dato real y decirlo.
