using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using Legacy.Services.IS_WS_PRUEBA.Contracts;
using Legacy.Services.IS_WS_PRUEBA.Domain.Entities;
using Legacy.Services.IS_WS_PRUEBA.Infrastructure;

namespace Legacy.Services.IS_WS_PRUEBA.Domain.Services
{
    public sealed class PruebaCrudService
    {
        private readonly IPruebaRepository _repository;

        public PruebaCrudService(IPruebaRepository repository)
        {
            _repository = repository;
        }

        public SoapResponseDto Crear(SoapRequestDto request)
        {
            try
            {
                Dictionary<string, SoapCampoDto> map;
                string error;
                if (!CampoParser.TryBuildFieldMap(request, out map, out error))
                {
                    return FunctionalError(error);
                }

                string nombre;
                if (!CampoParser.TryGetRequiredString(map, "nombre", out nombre, out error))
                {
                    return FunctionalError(error);
                }

                string descripcion;
                if (!CampoParser.TryGetRequiredString(map, "descripcion", out descripcion, out error))
                {
                    return FunctionalError(error);
                }

                DateTime fechaFundacion;
                if (!CampoParser.TryGetRequiredDate(map, "fecha_fundacion", out fechaFundacion, out error))
                {
                    return FunctionalError(error);
                }

                var created = _repository.Create(new PruebaRecord
                {
                    Nombre = nombre,
                    Descripcion = descripcion,
                    FechaFundacion = fechaFundacion,
                    Activo = true
                });

                return Success("Registro creado.", new[]
                {
                    Field("id", created.Id.ToString(CultureInfo.InvariantCulture), "int")
                });
            }
            catch (SqlException sqlException) when (IsUniqueNombreViolation(sqlException))
            {
                return FunctionalError("Ya existe un registro activo con ese nombre.");
            }
            catch (Exception exception)
            {
                Trace.TraceError("IS_WS_PRUEBA Crear failed: {0}", exception.Message);
                return TechnicalError();
            }
        }

        public SoapResponseDto Consultar(SoapRequestDto request)
        {
            try
            {
                Dictionary<string, SoapCampoDto> map;
                string error;
                if (!CampoParser.TryBuildFieldMap(request, out map, out error))
                {
                    return FunctionalError(error);
                }

                int id;
                if (!CampoParser.TryGetRequiredInt(map, "id", out id, out error))
                {
                    return FunctionalError(error);
                }

                var record = _repository.GetById(id);
                if (record == null || !record.Activo)
                {
                    return FunctionalError("Registro no encontrado.");
                }

                return Success("Consulta exitosa.", BuildOutput(record));
            }
            catch (Exception exception)
            {
                Trace.TraceError("IS_WS_PRUEBA Consultar failed: {0}", exception.Message);
                return TechnicalError();
            }
        }

        public SoapResponseDto Actualizar(SoapRequestDto request)
        {
            try
            {
                Dictionary<string, SoapCampoDto> map;
                string error;
                if (!CampoParser.TryBuildFieldMap(request, out map, out error))
                {
                    return FunctionalError(error);
                }

                int id;
                if (!CampoParser.TryGetRequiredInt(map, "id", out id, out error))
                {
                    return FunctionalError(error);
                }

                var existing = _repository.GetById(id);
                if (existing == null || !existing.Activo)
                {
                    return FunctionalError("Registro no encontrado.");
                }

                bool hasNombre;
                string nombre;
                if (!CampoParser.TryGetOptionalString(map, "nombre", out hasNombre, out nombre, out error))
                {
                    return FunctionalError(error);
                }

                bool hasDescripcion;
                string descripcion;
                if (!CampoParser.TryGetOptionalString(map, "descripcion", out hasDescripcion, out descripcion, out error))
                {
                    return FunctionalError(error);
                }

                bool hasFechaFundacion;
                DateTime fechaFundacion;
                if (!CampoParser.TryGetOptionalDate(map, "fecha_fundacion", out hasFechaFundacion, out fechaFundacion, out error))
                {
                    return FunctionalError(error);
                }

                if (!hasNombre && !hasDescripcion && !hasFechaFundacion)
                {
                    return FunctionalError("No update fields provided.");
                }

                if (hasNombre)
                {
                    existing.Nombre = nombre;
                }

                if (hasDescripcion)
                {
                    existing.Descripcion = descripcion;
                }

                if (hasFechaFundacion)
                {
                    existing.FechaFundacion = fechaFundacion;
                }

                var updated = _repository.Update(existing);
                if (updated == null)
                {
                    return FunctionalError("Registro no encontrado.");
                }

                return Success("Registro actualizado.", BuildOutput(updated));
            }
            catch (SqlException sqlException) when (IsUniqueNombreViolation(sqlException))
            {
                return FunctionalError("Ya existe un registro activo con ese nombre.");
            }
            catch (Exception exception)
            {
                Trace.TraceError("IS_WS_PRUEBA Actualizar failed: {0}", exception.Message);
                return TechnicalError();
            }
        }

        public SoapResponseDto Eliminar(SoapRequestDto request)
        {
            try
            {
                Dictionary<string, SoapCampoDto> map;
                string error;
                if (!CampoParser.TryBuildFieldMap(request, out map, out error))
                {
                    return FunctionalError(error);
                }

                int id;
                if (!CampoParser.TryGetRequiredInt(map, "id", out id, out error))
                {
                    return FunctionalError(error);
                }

                var existing = _repository.GetById(id);
                if (existing == null)
                {
                    return FunctionalError("Registro no encontrado.");
                }

                if (!existing.Activo)
                {
                    return FunctionalError("Registro ya eliminado.");
                }

                var deleted = _repository.Delete(id);
                if (!deleted)
                {
                    return FunctionalError("Registro no encontrado.");
                }

                return Success("Registro eliminado.", null);
            }
            catch (Exception exception)
            {
                Trace.TraceError("IS_WS_PRUEBA Eliminar failed: {0}", exception.Message);
                return TechnicalError();
            }
        }

        private static SoapResponseDto Success(string message, IEnumerable<SoapCampoDto> output)
        {
            var response = new SoapResponseDto
            {
                Codigo = "000",
                Mensaje = message
            };

            if (output != null)
            {
                response.ListaCamposSalida.AddRange(output);
            }

            return response;
        }

        private static SoapResponseDto FunctionalError(string message)
        {
            return new SoapResponseDto
            {
                Codigo = "001",
                Mensaje = message
            };
        }

        private static SoapResponseDto TechnicalError()
        {
            return new SoapResponseDto
            {
                Codigo = "900",
                Mensaje = "Technical error while processing the request."
            };
        }

        private static bool IsUniqueNombreViolation(SqlException sqlException)
        {
            return sqlException.Number == 2601 || sqlException.Number == 2627;
        }

        private static List<SoapCampoDto> BuildOutput(PruebaRecord record)
        {
            return new List<SoapCampoDto>
            {
                Field("id", record.Id.ToString(CultureInfo.InvariantCulture), "int"),
                Field("nombre", record.Nombre, "string"),
                Field("descripcion", record.Descripcion, "string"),
                Field("fecha_fundacion", record.FechaFundacion.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), "datetime"),
                Field("activo", record.Activo ? "true" : "false", "bool"),
                Field("fecha_actualizacion", record.FechaActualizacion.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), "datetime")
            };
        }

        private static SoapCampoDto Field(string name, string value, string type)
        {
            return new SoapCampoDto
            {
                Name = name,
                Value = value,
                Type = type
            };
        }
    }
}
