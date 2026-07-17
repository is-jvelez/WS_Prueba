# Blueprint — IS_WS_PRUEBA (WS_Prueba)

> Registro vivo de cambios funcionales sobre este servicio SOAP. Cada entrada resume
> qué se hizo, por qué, y el ROI estimado de tiempo IA vs implementación manual.
> Para pedir un cambio nuevo, usa `CHANGE_REQUEST_TEMPLATE.md`. La entrada más reciente
> va arriba.

---

## Registro de cambios

### CR-001 — Auditoría de cambios, unicidad de nombre y fix de fecha_fundacion

**Fecha:** 2026-07-17
**Rama:** `feature/prueba-CR-001-1`

#### Resumen ejecutivo

Con la persistencia ya real en SQL Server, se cerraron tres brechas: (1) las operaciones
que modifican datos (Crear/Actualizar/Eliminar) ahora dejan un rastro de auditoría
(operación, id, timestamp UTC) en una tabla nueva, escrito de forma atómica junto con la
operación principal — sin exponer todavía una forma de consultarlo; (2) ya no es posible
tener dos registros activos con el mismo `nombre` — la base de datos lo impide con un
índice único filtrado, y el servicio ahora devuelve un error funcional claro (`001`) en
vez de un error técnico genérico (`900`); (3) se corrigió que `fecha_fundacion` perdía la
hora en la respuesta SOAP — la causa real era un formateo de salida que truncaba a
`yyyy-MM-dd`, no la base de datos ni el parseo de entrada (que ya preservaban la hora
correctamente). Los tres cambios se verificaron contra el SQL Server real del
`docker-compose` y contra el servicio corriendo bajo IIS Express, sin romper ningún
comportamiento existente (incluyendo los registros semilla).

#### Alcance

**Dentro:**
- Tabla de auditoría (`dbo.PruebaAudit`) para Crear/Actualizar/Eliminar, solo almacenamiento.
- Índice único filtrado sobre `Nombre` para registros con `Activo=1`.
- Mapeo de la violación de unicidad a error funcional `001` en `Crear`/`Actualizar`.
- Fix de formateo de `fecha_fundacion` en la respuesta SOAP para preservar la hora.

**Fuera (decidido explícitamente durante la solicitud):**
- No se implementó una operación "Reactivar" — no existía en el código y se confirmó
  con el solicitante que el alcance de auditoría es solo sobre lo que ya existe hoy
  (Crear, Actualizar, Eliminar).
- No se expone consulta del historial de auditoría (solo almacenamiento).
- No se normalizan mayúsculas/acentos en `nombre`, ni se cambian los formatos de fecha
  aceptados en la entrada.

#### Archivos modificados

- `flyway/sql/V2__create_prueba_audit.sql` (nuevo)
- `flyway/sql/V3__add_prueba_nombre_unique_active.sql` (nuevo)
- `Infrastructure/SqlServerPruebaRepository.cs`
- `Domain/Services/PruebaCrudService.cs`
- `CHANGE_REQUEST_TEMPLATE.md` (nuevo, no es parte del cambio funcional pero se agregó
  en la misma sesión)

Sin cambios (a propósito): `IPruebaRepository.cs`, `InMemoryPruebaRepository.cs`
(código muerto, no referenciado en ningún punto de ejecución real), `IS_WS_PRUEBA.asmx.cs`,
`Contracts/*` (contrato SOAP intacto).

#### Incidente durante la verificación (vale la pena dejarlo anotado)

Una prueba manual inicial devolvió `900` en vez de `001` para un nombre duplicado. La
causa no fue el código, sino que había **dos copias del proyecto en la máquina**
(`...\IS\WS\E1\WS_Prueba`, la editada, y `...\Legacy\IS_WS_PRUEBA`, una copia vieja sin
el fix) y el IIS Express que atendía la prueba estaba sirviendo la copia vieja — ambas
apuntando a la misma base de datos, por lo que el constraint sí existía pero el código
que lo manejaba no. Se identificó vía `Get-CimInstance Win32_Process` sobre el proceso
`iisexpress.exe` (revela `/config` y `/site` reales) y se resolvió apuntando a la
instancia correcta (puerto `65526`, sitio `IS_WS_PRUEBA`). Ver el checklist de entorno en
`CHANGE_REQUEST_TEMPLATE.md` §7, agregado a raíz de este hallazgo.

#### ROI (tiempo IA vs manual estimado)

| | |
|---|---|
| Tiempo con IA (esta sesión) | ~60-75 min (estimación a partir de la actividad de la sesión: exploración de código, diseño validado con un segundo agente, implementación, reconstrucción y ejecución de migraciones Flyway, compilación, pruebas funcionales end-to-end contra SQL Server real, y diagnóstico del incidente de entorno) |
| Tiempo manual estimado (dev senior, sin IA) | ~6-9 horas |
| Ahorro estimado | ~85-90% |

**Justificación del estimado manual:** el punto más costoso no es escribir el código sino
el diagnóstico — rastrear `fecha_fundacion` por 4 capas (columna SQL, entidad, repositorio,
parser de entrada, formateo de salida) para aislar la causa real suele tomar 1-2h si no se
conoce el código de antemano; diseñar la escritura transaccional de auditoría (decidir
atomicidad, qué pasa en no-ops de Update/Delete) y su migración, 2-3h con pruebas; el
índice único filtrado + manejo correcto de `SqlException` (número de error correcto, no
confundir con condiciones de carrera legítimas) + pruebas, 1.5-2h; y las pruebas manuales
end-to-end vía SOAP más el tiempo de troubleshooting de entorno (que de hecho ocurrió en
esta misma sesión), 1-2h adicionales.

> Nota: esta es una estimación razonada, no una medición real de un desarrollador
> trabajando este mismo ticket sin IA. Si en el futuro se mide el tiempo manual real de
> un cambio comparable, reemplazar este valor y anotarlo como dato real.

---

<!-- Nuevas entradas: agregar arriba de esta línea, siguiendo el formato de CR-001. -->
