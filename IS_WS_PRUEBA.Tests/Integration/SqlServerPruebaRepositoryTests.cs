using System;
using System.Data;
using System.Data.SqlClient;
using Legacy.Services.IS_WS_PRUEBA.Domain.Entities;
using Legacy.Services.IS_WS_PRUEBA.Infrastructure;
using Xunit;

namespace Legacy.Services.IS_WS_PRUEBA.Tests.Integration
{
    public sealed class SqlServerPruebaRepositoryTests : IDisposable
    {
        private const string ConnectionString =
            "Server=localhost,15533;Database=IS_WS_PRUEBA;User Id=sa;Password=MiPassword123!2026;Encrypt=True;TrustServerCertificate=True;";

        private readonly SqlServerPruebaRepository _repository;
        private readonly System.Collections.Generic.List<int> _createdIds = new System.Collections.Generic.List<int>();

        public SqlServerPruebaRepositoryTests()
        {
            _repository = new SqlServerPruebaRepository(ConnectionString);
        }

        [Fact]
        public void Create_Exitoso_EscribeFilaDeAuditoriaConOperacionCrear()
        {
            var nombre = "CR001 Smoke Crear " + Guid.NewGuid();
            var created = _repository.Create(new PruebaRecord
            {
                Nombre = nombre,
                Descripcion = "smoke test CR-001",
                FechaFundacion = new DateTime(2020, 10, 15, 14, 30, 0, DateTimeKind.Utc)
            });
            _createdIds.Add(created.Id);

            Assert.Equal(1, CountAuditoria(created.Id, "Crear"));

            var reloaded = _repository.GetById(created.Id);
            Assert.Equal(new DateTime(2020, 10, 15, 14, 30, 0), reloaded.FechaFundacion);
        }

        [Fact]
        public void Create_ConNombreDuplicadoActivo_LanzaDuplicateNombreActivoExceptionYNoAuditoria()
        {
            var nombre = "CR001 Smoke Dup " + Guid.NewGuid();
            var first = _repository.Create(new PruebaRecord
            {
                Nombre = nombre,
                Descripcion = "smoke test CR-001",
                FechaFundacion = DateTime.UtcNow
            });
            _createdIds.Add(first.Id);

            var exception = Assert.Throws<DuplicateNombreActivoException>(() =>
                _repository.Create(new PruebaRecord
                {
                    Nombre = nombre,
                    Descripcion = "otro registro, mismo nombre activo",
                    FechaFundacion = DateTime.UtcNow
                }));

            Assert.Equal("Ya existe un registro activo con ese nombre.", exception.Message);

            Assert.Equal(1, CountAuditoria(first.Id, "Crear"));
        }

        [Fact]
        public void Delete_Exitoso_EscribeFilaDeAuditoriaConOperacionEliminar()
        {
            var nombre = "CR001 Smoke Eliminar " + Guid.NewGuid();
            var created = _repository.Create(new PruebaRecord
            {
                Nombre = nombre,
                Descripcion = "smoke test CR-001",
                FechaFundacion = DateTime.UtcNow
            });
            _createdIds.Add(created.Id);

            var deleted = _repository.Delete(created.Id);

            Assert.True(deleted);
            Assert.Equal(1, CountAuditoria(created.Id, "Eliminar"));
        }

        [Fact]
        public void Update_Exitoso_EscribeFilaDeAuditoriaConOperacionActualizar()
        {
            var created = _repository.Create(new PruebaRecord
            {
                Nombre = "CR001 Smoke Update " + Guid.NewGuid(),
                Descripcion = "smoke test CR-001 update",
                FechaFundacion = DateTime.UtcNow
            });
            _createdIds.Add(created.Id);

            created.Descripcion = "descripcion modificada";
            var updated = _repository.Update(created);

            Assert.NotNull(updated);
            Assert.Equal(1, CountAuditoria(created.Id, "Actualizar"));
        }

        [Fact]
        public void Create_ConNombreDuplicadoDeRegistroInactivo_TieneExito()
        {
            var nombre = "CR001 Smoke Inactivo " + Guid.NewGuid();
            var first = _repository.Create(new PruebaRecord
            {
                Nombre = nombre,
                Descripcion = "primer registro, se desactivara",
                FechaFundacion = DateTime.UtcNow
            });
            _createdIds.Add(first.Id);

            _repository.Delete(first.Id);

            var second = _repository.Create(new PruebaRecord
            {
                Nombre = nombre,
                Descripcion = "segundo registro, mismo nombre pero el primero esta inactivo",
                FechaFundacion = DateTime.UtcNow
            });
            _createdIds.Add(second.Id);

            Assert.NotEqual(first.Id, second.Id);
        }

        [Fact]
        public void Update_ReactivacionConNombreColisionandoConActivo_LanzaDuplicateNombreActivoException()
        {
            var nombreActivo = "CR001 Smoke Colision " + Guid.NewGuid();
            var activo = _repository.Create(new PruebaRecord
            {
                Nombre = nombreActivo,
                Descripcion = "registro activo existente",
                FechaFundacion = DateTime.UtcNow
            });
            _createdIds.Add(activo.Id);

            var inactivo = _repository.Create(new PruebaRecord
            {
                Nombre = "CR001 Smoke Colision Inactivo " + Guid.NewGuid(),
                Descripcion = "registro que se desactivara y luego reactivara con nombre colisionando",
                FechaFundacion = DateTime.UtcNow
            });
            _createdIds.Add(inactivo.Id);
            _repository.Delete(inactivo.Id);

            var reloaded = _repository.GetById(inactivo.Id);
            reloaded.Nombre = nombreActivo;
            reloaded.Activo = true;

            var exception = Assert.Throws<DuplicateNombreActivoException>(() => _repository.Update(reloaded));
            Assert.Equal("Ya existe un registro activo con ese nombre.", exception.Message);

            Assert.Equal(0, CountAuditoria(inactivo.Id, "Actualizar"));
        }

        private static int CountAuditoria(int pruebaId, string operacion)
        {
            const string sql = "SELECT COUNT(*) FROM dbo.PruebaAuditoria WHERE PruebaId = @PruebaId AND Operacion = @Operacion;";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@PruebaId", SqlDbType.Int).Value = pruebaId;
                command.Parameters.Add("@Operacion", SqlDbType.VarChar, 20).Value = operacion;
                connection.Open();
                return (int)command.ExecuteScalar();
            }
        }

        public void Dispose()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                foreach (var id in _createdIds)
                {
                    using (var command = new SqlCommand(
                        "DELETE FROM dbo.PruebaAuditoria WHERE PruebaId = @Id; DELETE FROM dbo.Prueba WHERE Id = @Id;",
                        connection))
                    {
                        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
