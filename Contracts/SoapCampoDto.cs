using System;
using System.Xml.Serialization;

namespace Legacy.Services.IS_WS_PRUEBA.Contracts
{
    [Serializable]
    [XmlType(TypeName = "Campo")]
    public class SoapCampoDto
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "value")]
        public string Value { get; set; }

        [XmlElement(ElementName = "type")]
        public string Type { get; set; }
    }
}
