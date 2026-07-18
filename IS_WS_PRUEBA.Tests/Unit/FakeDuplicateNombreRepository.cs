using Legacy.Services.IS_WS_PRUEBA.Domain.Entities;
using Legacy.Services.IS_WS_PRUEBA.Infrastructure;

namespace Legacy.Services.IS_WS_PRUEBA.Tests.Unit
{
    public sealed class FakeDuplicateNombreRepository : IPruebaRepository
    {
        private readonly PruebaRecord _existing;

        public FakeDuplicateNombreRepository(PruebaRecord existing = null)
        {
            _existing = existing;
        }

        public PruebaRecord Create(PruebaRecord record)
        {
            throw new DuplicateNombreActivoException(new System.Exception("simulated 2601"));
        }

        public PruebaRecord GetById(int id)
        {
            return _existing;
        }

        public PruebaRecord Update(PruebaRecord record)
        {
            throw new DuplicateNombreActivoException(new System.Exception("simulated 2601"));
        }

        public bool Delete(int id)
        {
            return true;
        }
    }
}
