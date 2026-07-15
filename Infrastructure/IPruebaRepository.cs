using Legacy.Services.IS_WS_PRUEBA.Domain.Entities;

namespace Legacy.Services.IS_WS_PRUEBA.Infrastructure
{
    public interface IPruebaRepository
    {
        PruebaRecord Create(PruebaRecord record);
        PruebaRecord GetById(int id);
        PruebaRecord Update(PruebaRecord record);
        bool Delete(int id);
    }
}
