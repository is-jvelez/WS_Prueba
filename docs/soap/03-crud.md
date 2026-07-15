# 03 - CRUD Implementation

## Status
- Result: PASS (functional implementation created)

## Implemented Operations
- Crear
- Consultar
- Actualizar
- Eliminar

## Business Flow
1. Build/validate `listaCampos` map.
2. Validate required fields and types.
3. Convert values (`int`, `datetime`, etc.).
4. Execute repository operation.
5. Return standard response (`000`, `001`, `900`).

## Persistence
- Repository: in-memory (`InMemoryPruebaRepository`)
- Scope: process-memory only
- Behavior:
  - create with auto-increment `id`
  - read by `id`
  - update fields
  - delete as logical delete (`Activo = false`)

## Output Convention
- `000`: success
- `001`: functional/validation error
- `900`: technical error

## Important Constraint
- SOAP contract field `value` is always `string`.
- Internal conversion is driven by `type`.
