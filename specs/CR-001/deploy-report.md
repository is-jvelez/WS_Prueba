# Deploy Report — CR-001

## Infraestructura de datos

### docker compose up -d --build
**Estado:** OK

- Imagen Flyway reconstruida: `ws_prueba-flyway` ✓
- Imagen SQL Server: `mcr.microsoft.com/mssql/server:2022-latest` ✓
- Red `ws_prueba_default` creada ✓
- Volumen `ws_prueba_sqlserver_data` creado ✓

### SQL Server healthcheck
**Estado:** HEALTHY (✓)

- Container: `is_ws_prueba_sqlserver` 
- Status: `Up 55 seconds (healthy)`
- Puerto host: 15533 (mapea a 1433 en contenedor)
- Cadena de conexión: `localhost,15533` (compatible con `Web.config`)

### Flyway migrations
**Estado:** COMPLETADO SIN ERRORES (✓)

- Container: `is_ws_prueba_flyway`
- Status: `Exited (0)` (service_completed_successfully)
- Tiempo de ejecución: 00:00.053s

Logs de ejecución:
```
Schema history table [IS_WS_PRUEBA].[dbo].[flyway_schema_history] does not exist yet
Successfully validated 3 migrations (execution time 00:00.097s)
Creating Schema History table [IS_WS_PRUEBA].[dbo].[flyway_schema_history] ...
Current version of schema [dbo]: << Empty Schema >>
Migrating schema [dbo] to version "1 - create prueba table"
Migrating schema [dbo] to version "2 - create prueba auditoria table"
Migrating schema [dbo] to version "3 - add unique index prueba nombre activo"
Successfully applied 3 migrations to schema [dbo], now at version v3 (execution time 00:00.053s)
```

### Migraciones del CR aplicadas
**Versiones aplicadas:** V1, V2, V3 ✓

| Versión | Archivo | Descripción | Status |
|---------|---------|-------------|--------|
| V1 | `V1__create_prueba_table.sql` | Crear tabla `dbo.Prueba` con columnas base | Aplicada ✓ |
| V2 | `V2__create_prueba_auditoria_table.sql` | Crear tabla `dbo.PruebaAuditoria` para historial de cambios | Aplicada ✓ |
| V3 | `V3__add_unique_index_prueba_nombre_activo.sql` | Crear índice único filtrado `UX_Prueba_Nombre_Activo` | Aplicada ✓ |

**Schema final en BD:**
- `dbo.Prueba` (Id, Nombre, Descripcion, FechaFundacion, Activo, FechaActualizacion)
- `dbo.PruebaAuditoria` (Id, PruebaId, Operacion, FechaUtc)
- Índice `UX_Prueba_Nombre_Activo` sobre `dbo.Prueba(Nombre)` filtrado por `Activo=1`

## Paso manual pendiente (fuera del alcance de este agente)

**Estado:** PENDIENTE MANUAL

El deployer automatiza **únicamente la capa de datos** (SQL Server + Flyway). La app `IS_WS_PRUEBA.asmx` (.NET Framework 4.8, SOAP/WebForms) **no corre en contenedor** y debe levantarse manualmente:

1. Abrir `IS_WS_PRUEBA.sln` en Visual Studio
2. Compilar solución: `dotnet build` o Build → Build Solution desde IDE
3. Verificar que `Web.config` apunta a la BD correcta:
   ```xml
   <add name="IS_WS_PRUEBAConnection" 
        connectionString="Server=localhost,15533;Database=IS_WS_PRUEBA;User Id=sa;Password=MiPassword123!2026" 
        providerName="System.Data.SqlClient" />
   ```
   - Host: `localhost`
   - Puerto: `15533` (variable `${SQLSERVER_HOST_PORT}` en `.env`, que ahora corre en contenedor en ese puerto)
4. Ejecutar con IIS Express o Visual Studio (Debug/F5)
5. Probar operaciones SOAP contra `http://localhost:PORT/IS_WS_PRUEBA.asmx`

**Nota:** El deployer no invoca IIS Express ni ejecuta la app porque no existe infraestructura de contenedor/CI para hacerlo de manera automática hoy. Este paso sigue siendo responsabilidad del desarrollador/entorno local.

## Verificación final

- ✓ `docker compose ps`: sqlserver healthy, flyway exited cleanly
- ✓ Todas las 3 migraciones del CR aplicadas sin errores de checksum
- ✓ BD `IS_WS_PRUEBA` creada y accesible en `localhost,15533`
- ✓ Schema `dbo` listo con tablas y restricciones acordadas
- ✓ Validación de fase 5 aprobada: `Estado: APROBADO`

## Estado: APROBADO

El CR-001 ha completado exitosamente la fase 6 de Deploy. La infraestructura de datos está lista para que la app `IS_WS_PRUEBA.asmx` se levante localmente apuntando a esta BD. No hay bloqueos técnicos pendientes en la capa de datos.

---

**Fecha:** 2026-07-17 18:48 UTC  
**Agente:** Phase 6 - Deploy (data infrastructure)  
**Infraestructura:** docker-compose (SQL Server 2022 + Flyway 10.22.0)  
**Migraciones:** V1, V2, V3 aplicadas exitosamente  
**Siguiente fase:** Cierre de CR por el orquestador (no requiere trabajo de este agente)
