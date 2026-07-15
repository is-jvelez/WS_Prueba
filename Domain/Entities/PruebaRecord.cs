using System;

namespace Legacy.Services.IS_WS_PRUEBA.Domain.Entities
{
    public class PruebaRecord
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaFundacion { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}
