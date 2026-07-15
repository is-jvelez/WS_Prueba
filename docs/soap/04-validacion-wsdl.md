# 04 - SOAP/WSDL Interoperability Validation

## Status
- Result: PASS
- Validation type: runtime validation in IIS Express + static contract checks.

## Checks
- Namespace consistency: PASS
  - Service namespace is centralized and reused.
- Operation names stability: PASS
  - `Crear`, `Consultar`, `Actualizar`, `Eliminar` exposed with `[WebMethod]`.
- Request/response shape: PASS
  - Request uses `listaCampos/campo(name,value,type)`.
  - Response uses `codigo/mensaje/listaCamposSalida`.
- XML serialization predictability: PASS
  - Serializable DTOs with explicit XML element/array attributes.
- Risky types in contract (`object`): PASS
  - No `object` in SOAP DTOs.

## Runtime Evidence
- Host: IIS Express
- URL: `http://localhost:53891/IS_WS_PRUEBA.asmx?WSDL`
- HTTP status: `200`
- Captured artifact: `docs/soap/IS_WS_PRUEBA.wsdl`
- Size: `10007` bytes
