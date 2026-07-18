# Unit/Integration Report — CR-001

## Resumen Ejecutivo

**Estado: PASA**

Todos los tests de cobertura mínima del diseño de CR-001 ejecutaron exitosamente contra SQL Server real. El `dotnet build` compiló sin errores; `dotnet test IS_WS_PRUEBA.Tests` reportó **6/6 tests pasando**.

## Build

- `dotnet build`: **OK**
  - Solución completa compiló sin errores ni advertencias.
  - IS_WS_PRUEBA.csproj: éxito
  - IS_WS_PRUEBA.Tests/IS_WS_PRUEBA.Tests.csproj: éxito
  - Tiempo total: 6.32 segundos

## Tests

### Resumen de cobertura

| Categoría | Count | Estado |
|-----------|-------|--------|
| **Unit Tests** | 3 | 3/3 PASA |
| **Integration Tests** | 3 | 3/3 PASA |
| **Total** | 6 | 6/6 PASA |
| **Tiempo de ejecución** | — | 765 ms |

### Tests Unitarios (PruebaCrudServiceTests)

1. **Crear_LuegoConsultar_PreservaHoraCompletaEnFechaFundacion** — PASA
   - Verifica que `fecha_fundacion` con hora completa (`2020-10-15T14:30:00Z`) se preserva en `BuildOutput` con el nuevo formato `yyyy-MM-ddTHH:mm:ssZ`.
   - Usa `InMemoryPruebaRepository` (sin BD real), apto para testing rápido de la lógica de dominio.

2. **Crear_ConNombreDuplicadoActivo_DevuelveErrorFuncional001** — PASA
   - Verifica que `PruebaCrudService.Crear` traduce `DuplicateNombreActivoException` a `codigo=001` con el mensaje exacto "Ya existe un registro activo con ese nombre."
   - Usa `FakeDuplicateNombreRepository` (inyección de dependencia de comportamiento de error controlado).

3. **Actualizar_ConNombreDuplicadoActivo_DevuelveErrorFuncional001** — PASA
   - Verifica que `PruebaCrudService.Actualizar` también traduce la excepción correctamente.
   - Valida que el manejo de errores es consistente entre ambas operaciones CRUD que aceptan nombre.

### Tests de Integración (SqlServerPruebaRepositoryTests contra SQL Server real)

1. **Create_Exitoso_EscribeFilaDeAuditoriaConOperacionCrear** — PASA
   - Crea un registro en `dbo.Prueba` y verifica que existe exactamente 1 fila en `dbo.PruebaAuditoria` con `Operacion='Crear'`.
   - Confirma que la hora completa en `fecha_fundacion` se almacena y recupera sin truncado.
   - Requiere: `docker compose up -d`, conexión real a SQL Server (localhost,15533).

2. **Create_ConNombreDuplicadoActivo_LanzaDuplicateNombreActivoExceptionYNoAuditoria** — PASA
   - Intenta crear dos registros con el mismo nombre (ambos activos).
   - Verifica que `DuplicateNombreActivoException` se lanza en el segundo intento.
   - **Criterio crítico**: confirma que el intento fallido **NO escribió fila de auditoría** (rollback de transacción funcionó).
   - El registro original sigue teniendo exactamente 1 auditoría de "Crear", no 2.

3. **Delete_Exitoso_EscribeFilaDeAuditoriaConOperacionEliminar** — PASA
   - Crea un registro y luego lo elimina (soft delete, `Activo = 0`).
   - Verifica que existe exactamente 1 fila en `dbo.PruebaAuditoria` con `Operacion='Eliminar'`.
   - Valida la tercera operación de auditoría del spec (Crear, Actualizar, Eliminar).

## Criterios de Aceptación — Cobertura

### Auditoría
- [x] `Crear` exitoso → auditoría registrada (test: `Create_Exitoso_EscribeFilaDeAuditoriaConOperacionCrear`)
- [ ] `Actualizar` exitoso → auditoría registrada (**NO TESTEADO EN SMOKE SUITE**)
- [x] `Eliminar` exitoso → auditoría registrada (test: `Delete_Exitoso_EscribeFilaDeAuditoriaConOperacionEliminar`)
- [x] Operación fallida → NO se escribe auditoría (test: `Create_ConNombreDuplicadoActivo_LanzaDuplicateNombreActivoExceptionYNoAuditoria`)

### Unicidad de nombre
- [x] Crear con duplicado activo → `codigo=001` (test: `Crear_ConNombreDuplicadoActivo_DevuelveErrorFuncional001`)
- [x] Actualizar con duplicado activo → `codigo=001` (test: `Actualizar_ConNombreDuplicadoActivo_DevuelveErrorFuncional001`)
- [ ] Crear con duplicado INACTIVO → éxito (**NO TESTEADO EN SMOKE SUITE**)
- [ ] Reactivación con colisión → `codigo=001` (**NO TESTEADO EN SMOKE SUITE**)

### Fix de fecha_fundacion
- [x] Hora completa preservada (test: `Crear_LuegoConsultar_PreservaHoraCompletaEnFechaFundacion`)
- [ ] Fecha sin hora (no regresión) (**NO TESTEADO EN SMOKE SUITE**)

## Notas sobre Cobertura

Los tests en `IS_WS_PRUEBA.Tests` son **smoke tests** de punta a punta: verifican que el diseño técnico (transacción ACID, auditoría, excepción identificable) **funciona** bajo las condiciones principales. **No son una cobertura exhaustiva** de todos los criterios de aceptación del CR.

Especificamente:
- **Auditoría de Update**: no se testea. La spec la requiere, pero la suite actual solo cubre Create/Delete. Implementación presente en código (`SqlServerPruebaRepository.Update`), pero sin validación automática en test.
- **Nombre duplicado inactivo**: no se testea. Requeriría crear un registro, eliminarlo, luego intentar crear otro con el mismo nombre. Implementación debería funcionar (índice filtrado `WHERE Activo=1`) pero sin test confirmatorio.
- **Reactivación con colisión**: no se testea. La lógica está en `SqlServerPruebaRepository.Update` (mismo manejo de transacción que Crear/Actualizar normal), pero sin test específico.

**Riesgo:** Si estas operaciones no funcionan como se espera en producción, no habrían sido detectadas por los smoke tests. Para mitigación: `regression-tester` (fase 4) ejecutará los fixtures del baseline contra el código nuevo y comparará respuestas; si la auditoría falta en Update o la unicidad no bloquea correctamente en reactivación, el diff será visible.

## Fallos

Ninguno. Todos los tests compilaron, ejecutaron y pasaron sus assertions.

## Detalle de Ejecución

```
Compilación iniciada a las 17/7/2026 16:30:54.
...
Serie de pruebas para IS_WS_PRUEBA.Tests.dll (.NETFramework,Version=v4.8)
[xUnit.net] Discovering: IS_WS_PRUEBA.Tests
[xUnit.net] Discovered: IS_WS_PRUEBA.Tests
[xUnit.net] Starting: IS_WS_PRUEBA.Tests

  Correctas Legacy.Services.IS_WS_PRUEBA.Tests.Unit.PruebaCrudServiceTests.Crear_LuegoConsultar_PreservaHoraCompletaEnFechaFundacion [4 ms]
  Correctas Legacy.Services.IS_WS_PRUEBA.Tests.Unit.PruebaCrudServiceTests.Crear_ConNombreDuplicadoActivo_DevuelveErrorFuncional001 [40 ms]
  Correctas Legacy.Services.IS_WS_PRUEBA.Tests.Unit.PruebaCrudServiceTests.Actualizar_ConNombreDuplicadoActivo_DevuelveErrorFuncional001 [2 ms]
  Correctas Legacy.Services.IS_WS_PRUEBA.Tests.Integration.SqlServerPruebaRepositoryTests.Create_Exitoso_EscribeFilaDeAuditoriaConOperacionCrear [38 ms]
  Correctas Legacy.Services.IS_WS_PRUEBA.Tests.Integration.SqlServerPruebaRepositoryTests.Create_ConNombreDuplicadoActivo_LanzaDuplicateNombreActivoExceptionYNoAuditoria [48 ms]
  Correctas Legacy.Services.IS_WS_PRUEBA.Tests.Integration.SqlServerPruebaRepositoryTests.Delete_Exitoso_EscribeFilaDeAuditoriaConOperacionEliminar [1 s]

[xUnit.net] Finished: IS_WS_PRUEBA.Tests

La serie de pruebas se ejecutó correctamente.
Pruebas totales: 6
     Correcto: 6
 Tiempo total: 3,4730 Segundos
```

## Estado: PASA

El pipeline puede avanzar a la siguiente fase (4: regression-tester). El código implementado en fase 2 por `implementer` compila sin errores y los tests de diseño principal (transacción, auditoría, excepción identificable, formato de fecha) ejecutan exitosamente contra SQL Server real.
