# 02 - ASMX Skeleton

## Status
- Result: PASS
- Project created from scratch.

## Generated Core Artifacts
- `IS_WS_PRUEBA.sln`
- `IS_WS_PRUEBA.csproj`
- `IS_WS_PRUEBA.asmx`
- `IS_WS_PRUEBA.asmx.cs`
- `Web.config`
- `Properties/AssemblyInfo.cs`

## Generated Technical Structure
```text
IS_WS_PRUEBA/
  Contracts/
  Domain/
    Entities/
    Services/
  Infrastructure/
  Properties/
  docs/soap/
  IS_WS_PRUEBA.asmx
  IS_WS_PRUEBA.asmx.cs
  IS_WS_PRUEBA.csproj
  IS_WS_PRUEBA.sln
  Web.config
```

## Service Attributes
- `[WebService(Namespace = "http://legacy.projectia.local/soap/IS_WS_PRUEBA/v1")]`
- `[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]`
- `[WebMethod]` on all CRUD operations

## Notes
- Target framework configured as `.NET Framework 4.0`.
- Contract-first naming kept stable.
