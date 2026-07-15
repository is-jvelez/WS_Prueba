using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Legacy.Services.IS_WS_PRUEBA.Domain.Entities;

namespace Legacy.Services.IS_WS_PRUEBA.Infrastructure
{
    public sealed class SqlServerPruebaRepository : IPruebaRepository
    {
        private readonly string _connectionString;

        public SqlServerPruebaRepository()
            : this(ConfigurationManager.ConnectionStrings["IS_WS_PRUEBAConnection"].ConnectionString)
        {
        }

        public SqlServerPruebaRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'IS_WS_PRUEBAConnection' is not configured.");
            }

            _connectionString = connectionString;
        }

        public PruebaRecord Create(PruebaRecord record)
        {
            const string sql = @"
INSERT INTO dbo.Prueba (Nombre, Descripcion, FechaFundacion, Activo, FechaActualizacion)
OUTPUT INSERTED.Id, INSERTED.Nombre, INSERTED.Descripcion, INSERTED.FechaFundacion, INSERTED.Activo, INSERTED.FechaActualizacion
VALUES (@Nombre, @Descripcion, @FechaFundacion, 1, SYSUTCDATETIME());";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@Nombre", SqlDbType.NVarChar, 200).Value = record.Nombre;
                command.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 1000).Value = record.Descripcion;
                command.Parameters.Add("@FechaFundacion", SqlDbType.DateTime2).Value = record.FechaFundacion;

                connection.Open();
                using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
                {
                    reader.Read();
                    return Map(reader);
                }
            }
        }

        public PruebaRecord GetById(int id)
        {
            const string sql = @"
SELECT Id, Nombre, Descripcion, FechaFundacion, Activo, FechaActualizacion
FROM dbo.Prueba
WHERE Id = @Id;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                connection.Open();
                using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
                {
                    return reader.Read() ? Map(reader) : null;
                }
            }
        }

        public PruebaRecord Update(PruebaRecord record)
        {
            const string sql = @"
UPDATE dbo.Prueba
SET Nombre = @Nombre,
    Descripcion = @Descripcion,
    FechaFundacion = @FechaFundacion,
    Activo = @Activo,
    FechaActualizacion = SYSUTCDATETIME()
OUTPUT INSERTED.Id, INSERTED.Nombre, INSERTED.Descripcion, INSERTED.FechaFundacion, INSERTED.Activo, INSERTED.FechaActualizacion
WHERE Id = @Id;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = record.Id;
                command.Parameters.Add("@Nombre", SqlDbType.NVarChar, 200).Value = record.Nombre;
                command.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 1000).Value = record.Descripcion;
                command.Parameters.Add("@FechaFundacion", SqlDbType.DateTime2).Value = record.FechaFundacion;
                command.Parameters.Add("@Activo", SqlDbType.Bit).Value = record.Activo;

                connection.Open();
                using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
                {
                    return reader.Read() ? Map(reader) : null;
                }
            }
        }

        public bool Delete(int id)
        {
            const string sql = @"
UPDATE dbo.Prueba
SET Activo = 0,
    FechaActualizacion = SYSUTCDATETIME()
WHERE Id = @Id;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
        }

        private static PruebaRecord Map(SqlDataReader reader)
        {
            return new PruebaRecord
            {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Descripcion = reader.GetString(2),
                FechaFundacion = reader.GetDateTime(3),
                Activo = reader.GetBoolean(4),
                FechaActualizacion = reader.GetDateTime(5)
            };
        }
    }
}
