using System;
using System.Collections.Generic;
using Legacy.Services.IS_WS_PRUEBA.Domain.Entities;

namespace Legacy.Services.IS_WS_PRUEBA.Infrastructure
{
    public sealed class InMemoryPruebaRepository : IPruebaRepository
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<int, PruebaRecord> Records = new Dictionary<int, PruebaRecord>();
        private static int _nextId = 1;

        private static readonly InMemoryPruebaRepository InstanceValue = new InMemoryPruebaRepository();

        public static InMemoryPruebaRepository Instance
        {
            get { return InstanceValue; }
        }

        private InMemoryPruebaRepository()
        {
        }

        public PruebaRecord Create(PruebaRecord record)
        {
            lock (Sync)
            {
                record.Id = _nextId;
                _nextId++;
                record.Activo = true;
                record.FechaActualizacion = DateTime.UtcNow;
                Records[record.Id] = Clone(record);
                return Clone(record);
            }
        }

        public PruebaRecord GetById(int id)
        {
            lock (Sync)
            {
                PruebaRecord existing;
                if (!Records.TryGetValue(id, out existing))
                {
                    return null;
                }

                return Clone(existing);
            }
        }

        public PruebaRecord Update(PruebaRecord record)
        {
            lock (Sync)
            {
                PruebaRecord existing;
                if (!Records.TryGetValue(record.Id, out existing))
                {
                    return null;
                }

                existing.Nombre = record.Nombre;
                existing.Descripcion = record.Descripcion;
                existing.FechaFundacion = record.FechaFundacion;
                existing.Activo = record.Activo;
                existing.FechaActualizacion = DateTime.UtcNow;

                Records[existing.Id] = Clone(existing);
                return Clone(existing);
            }
        }

        public bool Delete(int id)
        {
            lock (Sync)
            {
                PruebaRecord existing;
                if (!Records.TryGetValue(id, out existing))
                {
                    return false;
                }

                existing.Activo = false;
                existing.FechaActualizacion = DateTime.UtcNow;
                Records[id] = Clone(existing);
                return true;
            }
        }

        private static PruebaRecord Clone(PruebaRecord record)
        {
            if (record == null)
            {
                return null;
            }

            return new PruebaRecord
            {
                Id = record.Id,
                Nombre = record.Nombre,
                Descripcion = record.Descripcion,
                FechaFundacion = record.FechaFundacion,
                Activo = record.Activo,
                FechaActualizacion = record.FechaActualizacion
            };
        }
    }
}
