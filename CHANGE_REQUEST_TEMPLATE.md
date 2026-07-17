# Plantilla de solicitud de cambio — IS_WS_PRUEBA

> Copia este archivo (o su contenido) al pedir un cambio nuevo sobre este servicio.
> Entre más completes las secciones, menos idas y vueltas necesita la IA para entender
> el alcance real. Las secciones marcadas **(obligatorio)** son las que más evitan
> retrabajo; el resto ayuda pero se puede omitir si no aplica.

## 0. Identificación

- **CR / Ticket:** CR-002-1 (`feature/prueba-CR-002-1`)
- **Fecha de solicitud:** 2026-07-17
- **Solicitante:** Jose Velez

## 1. Contexto y motivación **(obligatorio)**

se realiza esto para tener la auditoria por id de registro es decir por empresa, para saber que 
operaciones se realizaron alo largo de su historial.

## 2. Objetivo **(obligatorio)**

Por consultar historial de cambios de una empresa

## 3. Alcance

### Dentro de alcance **(obligatorio si hay ambigüedad)**
- Utilizar infraestructura existente, trata de no quemar codigo SQL desde .net.
- Esta es una operación **Consultar nueva** (no existe todavía en el código): expone por
  primera vez, vía SOAP, la tabla `dbo.PruebaAudit` creada en CR-001 (hasta ahora solo se
  escribía, nunca se consultaba). Se llamará al nuevo `WebMethod` **`ConsultarHistorial`**.
- La consulta se resuelve con una **stored procedure nueva** (`dbo.usp_ConsultarHistorialEmpresa`,
  creada en una migración Flyway nueva), en vez de SQL parametrizado inline en C# — es un
  patrón nuevo para este servicio (hoy no existe ninguna SP; todo el acceso a datos usa SQL
  inline), decidido explícitamente para este cambio para evitar "quemar" el SQL de consulta
  en .NET.
- Recibe `id` (el id de la empresa/`Prueba`) como único campo de entrada, igual que
  `Consultar`. Devuelve una fila por operación de auditoría (`Crear`/`Actualizar`/`Eliminar`)
  encontrada, más reciente primero. Como `listaCamposSalida` es una lista plana de
  `campo` (name/value/type) sin agrupamiento nativo, el formato de salida acordado es
  **campos indexados**: `total` (cantidad de filas) y luego, por cada fila `N` (1-based):
  `id_N`, `operacion_N`, `fecha_N`.

### Fuera de alcance **(obligatorio si hay ambigüedad)**
- NO toque lo que ya funciona acctualmente
- No se modifica el contrato de los `WebMethods` existentes (`Crear`/`Consultar`/`Actualizar`/`Eliminar`),
  ni `IPruebaRepository`, ni `InMemoryPruebaRepository` (código muerto, ver CR-001).
- No se agrega paginación ni filtros adicionales (rango de fechas, tipo de operación) —
  se devuelve el historial completo de la empresa consultada.

> Si el cambio toca una operación (Crear/Consultar/Actualizar/Eliminar/otra) que **no
> existe todavía** en el código, dilo explícitamente — no asumas que la IA debe crearla.

## 4. Si es un bug: diagnóstico esperado antes del fix

- Confirmar qué capas **ya funcionan bien** para no tocarlas de más.

## 5. Restricciones que no se deben romper **(obligatorio)**

Marca las que apliquen (son las más comunes en este servicio):
- [x] No cambiar el contrato SOAP existente (WebMethods, `SoapRequestDto`/`SoapResponseDto`,
      nombres/tipos de `campo`) salvo que se pida explícitamente.
- [x] No degradar tiempo de respuesta de forma perceptible.
- [x] Todo cambio de esquema va en una migración Flyway nueva (`flyway/sql/V{n}__*.sql`),
      nunca editando una migración ya aplicada.
- [x] Mantener el patrón de códigos de respuesta: `000` éxito, `001` error funcional
      (validación/negocio — mensaje claro para el consumidor), `900` error técnico
      (catch-all, sin detalle interno expuesto).
- [x] No romper datos existentes (seed u otros registros ya persistidos).
- [x] No permite actualizar datos del historial, solo consulta

## 6. Criterios de aceptación **(obligatorio)**

Lista concreta de comportamientos esperados, en términos de qué `codigo`/`mensaje`/campos
de salida debe devolver cada escenario relevante (éxito, error funcional, edge cases).
- `000` - Consulta exitosa. `listaCamposSalida` trae `total` + `id_N`/`operacion_N`/`fecha_N`
  por cada operación de auditoría encontrada para la empresa, ordenadas de más reciente a
  más antigua.
- `001` - No existe historial para el `id` de empresa recibido (sin filas en `PruebaAudit`,
  exista o no la empresa) — mensaje funcional claro, no error técnico.
- `001` - Falta o es inválido el campo `id` de entrada (mismo tratamiento que en `Consultar`).
- `900` - Error técnico catch-all (se corrige el `999` que había quedado escrito antes; el
  patrón de este servicio, confirmado también en la restricción marcada arriba, es
  `000`/`001`/`900`, el mismo que ya usa CR-001).

## 7. Entorno de pruebas — checklist para evitar falsos negativos

> Ya nos pasó: existe más de una copia de este proyecto en la máquina, cada una con su
> propio sitio de IIS Express. Antes de reportar "esto no funcionó", verifica:

- [ ] La carpeta que se editó es la misma que sirve el sitio contra el que se prueba
      (confirmar con el PID/`CommandLine` de `iisexpress.exe` o el puerto real usado).
- [ ] El contenedor Docker de SQL Server está corriendo y sano
      (`docker compose ps` / healthcheck).
- [x] Si se agregaron migraciones nuevas, se reconstruyó la imagen de Flyway antes de
      migrar (`docker compose build flyway`) — el Dockerfile copia `sql/` al build, así
      que una imagen vieja no ve migraciones nuevas. Lo que pasa es que agregue datos es decir que utilice soap para ingresar empresa y modificarlas y elimnarla
       
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
