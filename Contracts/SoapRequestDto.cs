using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Legacy.Services.IS_WS_PRUEBA.Contracts
{
    [Serializable]
    [XmlType(TypeName = "Solicitud")]
    public class SoapRequestDto
    {
        public SoapRequestDto()
        {
            ListaCampos = new List<SoapCampoDto>();
        }

        [XmlArray(ElementName = "listaCampos")]
        [XmlArrayItem(ElementName = "campo")]
        public List<SoapCampoDto> ListaCampos { get; set; }
    }
}
