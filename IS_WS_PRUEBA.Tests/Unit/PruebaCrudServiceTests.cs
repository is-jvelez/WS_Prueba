using System.Collections.Generic;
using Legacy.Services.IS_WS_PRUEBA.Contracts;
using Legacy.Services.IS_WS_PRUEBA.Domain.Entities;
using Legacy.Services.IS_WS_PRUEBA.Domain.Services;
using Legacy.Services.IS_WS_PRUEBA.Infrastructure;
using Xunit;

namespace Legacy.Services.IS_WS_PRUEBA.Tests.Unit
{
    public class PruebaCrudServiceTests
    {
        [Fact]
        public void Crear_LuegoConsultar_PreservaHoraCompletaEnFechaFundacion()
        {
            var service = new PruebaCrudService(InMemoryPruebaRepository.Instance);

            var crearResponse = service.Crear(BuildRequest(new[]
            {
                Campo("nombre", "Empresa Test CR-001 Fecha", "string"),
                Campo("descripcion", "smoke test", "string"),
                Campo("fecha_fundacion", "2020-10-15T14:30:00Z", "datetime")
            }));

            Assert.Equal("000", crearResponse.Codigo);
            var id = crearResponse.ListaCamposSalida[0].Value;

            var consultarResponse = service.Consultar(BuildRequest(new[]
            {
                Campo("id", id, "int")
            }));

            Assert.Equal("000", consultarResponse.Codigo);
            var fechaFundacionCampo = consultarResponse.ListaCamposSalida.Find(c => c.Name == "fecha_fundacion");
            Assert.Equal("2020-10-15T14:30:00Z", fechaFundacionCampo.Value);
        }

        [Fact]
        public void Crear_ConNombreDuplicadoActivo_DevuelveErrorFuncional001()
        {
            var service = new PruebaCrudService(new FakeDuplicateNombreRepository());

            var response = service.Crear(BuildRequest(new[]
            {
                Campo("nombre", "Empresa XYZ", "string"),
                Campo("descripcion", "smoke test", "string"),
                Campo("fecha_fundacion", "2020-01-01", "datetime")
            }));

            Assert.Equal("001", response.Codigo);
            Assert.Equal("Ya existe un registro activo con ese nombre.", response.Mensaje);
        }

        [Fact]
        public void Actualizar_ConNombreDuplicadoActivo_DevuelveErrorFuncional001()
        {
            var existing = new PruebaRecord
            {
                Id = 1,
                Nombre = "Empresa Original",
                Descripcion = "desc",
                FechaFundacion = System.DateTime.UtcNow,
                Activo = true,
                FechaActualizacion = System.DateTime.UtcNow
            };
            var service = new PruebaCrudService(new FakeDuplicateNombreRepository(existing));

            var response = service.Actualizar(BuildRequest(new[]
            {
                Campo("id", "1", "int"),
                Campo("nombre", "Empresa XYZ", "string")
            }));

            Assert.Equal("001", response.Codigo);
            Assert.Equal("Ya existe un registro activo con ese nombre.", response.Mensaje);
        }

        private static SoapRequestDto BuildRequest(IEnumerable<SoapCampoDto> campos)
        {
            var request = new SoapRequestDto();
            request.ListaCampos.AddRange(campos);
            return request;
        }

        private static SoapCampoDto Campo(string name, string value, string type)
        {
            return new SoapCampoDto { Name = name, Value = value, Type = type };
        }
    }
}
