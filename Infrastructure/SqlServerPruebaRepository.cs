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
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        PruebaRecord created;
                        using (var command = new SqlCommand(sql, connection, transaction))
                        {
                            command.Parameters.Add("@Nombre", SqlDbType.NVarChar, 200).Value = record.Nombre;
                            command.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 1000).Value = record.Descripcion;
                            command.Parameters.Add("@FechaFundacion", SqlDbType.DateTime2).Value = record.FechaFundacion;

                            using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
                            {
                                reader.Read();
                                created = Map(reader);
                            }
                        }

                        WriteAudit(connection, transaction, "Crear", created.Id);
                        transaction.Commit();
                        return created;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
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
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        PruebaRecord updated = null;
                        using (var command = new SqlCommand(sql, connection, transaction))
                        {
                            command.Parameters.Add("@Id", SqlDbType.Int).Value = record.Id;
                            command.Parameters.Add("@Nombre", SqlDbType.NVarChar, 200).Value = record.Nombre;
                            command.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 1000).Value = record.Descripcion;
                            command.Parameters.Add("@FechaFundacion", SqlDbType.DateTime2).Value = record.FechaFundacion;
                            command.Parameters.Add("@Activo", SqlDbType.Bit).Value = record.Activo;

                            using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
                            {
                                if (reader.Read())
                                {
                                    updated = Map(reader);
                                }
                            }
                        }

                        if (updated != null)
                        {
                            WriteAudit(connection, transaction, "Actualizar", updated.Id);
                        }

                        transaction.Commit();
                        return updated;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
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
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        bool deleted;
                        using (var command = new SqlCommand(sql, connection, transaction))
                        {
                            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                            deleted = command.ExecuteNonQuery() > 0;
                        }

                        if (deleted)
                        {
                            WriteAudit(connection, transaction, "Eliminar", id);
                        }

                        transaction.Commit();
                        return deleted;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, string operacion, int pruebaId)
        {
            const string sql = "INSERT INTO dbo.PruebaAudit (Operacion, PruebaId) VALUES (@Operacion, @PruebaId);";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@Operacion", SqlDbType.NVarChar, 20).Value = operacion;
                command.Parameters.Add("@PruebaId", SqlDbType.Int).Value = pruebaId;
                command.ExecuteNonQuery();
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
