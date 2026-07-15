using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Web.Services;
using Legacy.Services.IS_WS_PRUEBA.Contracts;
using Legacy.Services.IS_WS_PRUEBA.Domain.Services;
using Legacy.Services.IS_WS_PRUEBA.Infrastructure;

namespace Legacy.Services.IS_WS_PRUEBA
{
    [WebService(Namespace = ServiceContractMetadata.ServiceNamespace)]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class IS_WS_PRUEBA : WebService
    {
        private readonly PruebaCrudService _crudService;

        public IS_WS_PRUEBA()
        {
            _crudService = new PruebaCrudService(InMemoryPruebaRepository.Instance);
        }

        [WebMethod(Description = "Creates a record from listaCampos input")]
        public SoapResponseDto Crear(SoapRequestDto request)
        {
            return ExecuteSafely("Crear", delegate { return _crudService.Crear(request); });
        }

        [WebMethod(Description = "Reads a record by id from listaCampos input")]
        public SoapResponseDto Consultar(SoapRequestDto request)
        {
            return ExecuteSafely("Consultar", delegate { return _crudService.Consultar(request); });
        }

        [WebMethod(Description = "Updates a record by id from listaCampos input")]
        public SoapResponseDto Actualizar(SoapRequestDto request)
        {
            return ExecuteSafely("Actualizar", delegate { return _crudService.Actualizar(request); });
        }

        [WebMethod(Description = "Deletes a record by id from listaCampos input")]
        public SoapResponseDto Eliminar(SoapRequestDto request)
        {
            return ExecuteSafely("Eliminar", delegate { return _crudService.Eliminar(request); });
        }

        private static SoapResponseDto ExecuteSafely(string operation, Func<SoapResponseDto> action)
        {
            try
            {
                return action();
            }
            catch (Exception exception)
            {
                Trace.TraceError("IS_WS_PRUEBA operation {0} failed: {1}", operation, exception.Message);
                return new SoapResponseDto
                {
                    Codigo = "900",
                    Mensaje = "Technical error while processing the request."
                };
            }
        }
    }
}
