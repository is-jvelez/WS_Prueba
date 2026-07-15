using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Legacy.Services.IS_WS_PRUEBA.Contracts
{
    [Serializable]
    [XmlType(TypeName = "Respuesta")]
    public class SoapResponseDto
    {
        public SoapResponseDto()
        {
            Codigo = "900";
            Mensaje = "Unexpected technical error.";
            ListaCamposSalida = new List<SoapCampoDto>();
        }

        [XmlElement(ElementName = "codigo")]
        public string Codigo { get; set; }

        [XmlElement(ElementName = "mensaje")]
        public string Mensaje { get; set; }

        [XmlArray(ElementName = "listaCamposSalida")]
        [XmlArrayItem(ElementName = "campo")]
        public List<SoapCampoDto> ListaCamposSalida { get; set; }
    }
}
