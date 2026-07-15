# 01 - SOAP Contract (IS_WS_PRUEBA)

## Service Identity
- Service Name: `IS_WS_PRUEBA`
- Namespace: `http://legacy.projectia.local/soap/IS_WS_PRUEBA/v1`
- Style: SOAP/ASMX (document-literal by default in ASMX)

## Service Type
- Classification: CRUD real with command-like response envelope.
- Reason: `Consultar` returns resource data in `listaCamposSalida`.

## Operations
- `Crear(request)`
- `Consultar(request)`
- `Actualizar(request)`
- `Eliminar(request)`

## Input Contract
- Envelope: `listaCampos` (array)
- Each item (`campo`):
  - `name` (string)
  - `value` (string)
  - `type` (string)

## Output Contract
- `codigo`: `000|001|900`
- `mensaje`: text
- `listaCamposSalida`: optional output fields for read/update/create

## Required Fields by Operation
- Crear: `nombre:string`, `descripcion:string`, `fecha_fundacion:datetime`
- Consultar: `id:int`
- Actualizar: `id:int` plus at least one of `nombre`, `descripcion`, `fecha_fundacion`
- Eliminar: `id:int`

## Date Rules
- Accepted formats:
  - `yyyy-MM-dd`
  - `yyyy-MM-ddTHH:mm:ss`
  - `yyyy-MM-ddTHH:mm:ssZ`

## Example Request XML (Crear)
```xml
<soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
               xmlns:xsd="http://www.w3.org/2001/XMLSchema"
               xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <Crear xmlns="http://legacy.projectia.local/soap/IS_WS_PRUEBA/v1">
      <request>
        <listaCampos>
          <campo>
            <name>nombre</name>
            <value>Empresa XYZ</value>
            <type>string</type>
          </campo>
          <campo>
            <name>descripcion</name>
            <value>Proveedor nacional</value>
            <type>string</type>
          </campo>
          <campo>
            <name>fecha_fundacion</name>
            <value>2020-10-15</value>
            <type>datetime</type>
          </campo>
        </listaCampos>
      </request>
    </Crear>
  </soap:Body>
</soap:Envelope>
```

## Example Response XML (Success)
```xml
<CrearResponse xmlns="http://legacy.projectia.local/soap/IS_WS_PRUEBA/v1">
  <CrearResult>
    <codigo>000</codigo>
    <mensaje>Registro creado.</mensaje>
    <listaCamposSalida>
      <campo>
        <name>id</name>
        <value>1</value>
        <type>int</type>
      </campo>
    </listaCamposSalida>
  </CrearResult>
</CrearResponse>
```
