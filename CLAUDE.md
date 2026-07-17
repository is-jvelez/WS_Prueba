# WS_Prueba — contexto para Claude Code

## Qué es este proyecto
Web service legacy .NET 4.8 (SOAP/WebForms, `IS_WS_PRUEBA.asmx`) con una capa de arquitectura hexagonal parcial ya construida:
- `Contracts/` — DTOs SOAP (`ServiceContractMetadata.cs`, `SoapCampoDto.cs`, `SoapRequestDto.cs`, `SoapResponseDto.cs`)
- `Domain/Entities/` y `Domain/Services/` — lógica de negocio
- `Infrastructure/` — repositorios contra SQL Server (`IPruebaRepository.cs`, `SqlServerPruebaRepository.cs`, `InMemoryPruebaRepository.cs` para tests)
- `flyway/` — migraciones versionadas de base de datos
- `IS_WS_PRUEBA.Tests` — unit + integration tests
- SQL Server corre en contenedor vía `docker-compose.yml`; Flyway migra al levantar el entorno

<!-- TODO (José): agrega aquí 3-5 líneas con las convenciones reales del proyecto que
     hoy no están escritas en ningún lado — ej: convención de nombres de migraciones
     Flyway, si los repositorios usan Dapper/ADO puro/EF, patrón de manejo de errores
     de negocio en las respuestas SOAP, etc. Sin esto, cada agente puede inventar su
     propia convención y vas a terminar con estilos mezclados. -->

## Regla dura: todo feature pasa por el pipeline
Cualquier cambio de código en `Domain/`, `Infrastructure/` o `Contracts/` DEBE originarse desde el comando `/feature "<change request>"`, que ejecuta las 5 fases (spec → behavior-capture → implementer → validator → release) definidas en `.claude/agents/`.

No edites estos directorios directamente en una conversación normal fuera de `/feature`, ni saltes fases del pipeline aunque el cambio parezca trivial — el behavior-capture existe justamente para los cambios que "parecen triviales" en un sistema legacy.

## Comandos habituales
```bash
docker compose up -d          # levanta SQL Server + aplica migraciones Flyway
docker compose down -v        # baja el entorno y borra el volumen (para pruebas limpias)
dotnet build
dotnet test IS_WS_PRUEBA.Tests
```

<!-- TODO (José): si hay algún comando adicional que siempre corres (ej. seed de datos,
     script para regenerar el WSDL, variables de entorno obligatorias en .env), agrégalo
     aquí. Los agentes van a asumir que estos son los únicos comandos válidos. -->

## Dónde viven los artefactos de cada change request
`specs/CR-XXX/spec.md`, `baseline-snapshot.md`, `fixtures/`, `validation-report.md` — no se crean a mano, los genera el pipeline.