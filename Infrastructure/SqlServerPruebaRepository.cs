using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Legacy.Services.IS_WS_PRUEBA.Domain.Entities;

namespace Legacy.Services.IS_WS_PRUEBA.Infrastructure
{
    /// <summary>
    /// Excepción identificable para la violación del índice único filtrado
    /// UX_Prueba_Nombre_Activo (dbo.Prueba(Nombre) WHERE Activo = 1). Permite a
    /// PruebaCrudService.Crear/Actualizar traducirla a un error funcional (codigo=001)
    /// en vez de dejar que se propague como error técnico genérico (codigo=900).
    /// </summary>
    public sealed class DuplicateNombreActivoException : Exception
    {
        public DuplicateNombreActivoException(Exception inner)
            : base("Ya existe un registro activo con ese nombre.", inner)
        {
        }
    }

    public sealed class SqlServerPruebaRepository : IPruebaRepository
    {
        private const string UniqueIndexName = "UX_Prueba_Nombre_Activo";

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
            const string insertSql = @"
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
                        using (var command = new SqlCommand(insertSql, connection, transaction))
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

                        InsertAuditoria(connection, transaction, created.Id, "Crear");

                        transaction.Commit();
                        return created;
                    }
                    catch (SqlException sqlException)
                    {
                        RollbackSafely(transaction);

                        if (IsUniqueNombreActivoViolation(sqlException))
                        {
                            throw new DuplicateNombreActivoException(sqlException);
                        }

                        throw;
                    }
                    catch
                    {
                        RollbackSafely(transaction);
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
            const string updateSql = @"
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
                        PruebaRecord updated;
                        using (var command = new SqlCommand(updateSql, connection, transaction))
                        {
                            command.Parameters.Add("@Id", SqlDbType.Int).Value = record.Id;
                            command.Parameters.Add("@Nombre", SqlDbType.NVarChar, 200).Value = record.Nombre;
                            command.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 1000).Value = record.Descripcion;
                            command.Parameters.Add("@FechaFundacion", SqlDbType.DateTime2).Value = record.FechaFundacion;
                            command.Parameters.Add("@Activo", SqlDbType.Bit).Value = record.Activo;

                            using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
                            {
                                updated = reader.Read() ? Map(reader) : null;
                            }
                        }

                        if (updated == null)
                        {
                            transaction.Commit();
                            return null;
                        }

                        InsertAuditoria(connection, transaction, updated.Id, "Actualizar");

                        transaction.Commit();
                        return updated;
                    }
                    catch (SqlException sqlException)
                    {
                        RollbackSafely(transaction);

                        if (IsUniqueNombreActivoViolation(sqlException))
                        {
                            throw new DuplicateNombreActivoException(sqlException);
                        }

                        throw;
                    }
                    catch
                    {
                        RollbackSafely(transaction);
                        throw;
                    }
                }
            }
        }

        public bool Delete(int id)
        {
            const string deleteSql = @"
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
                        int rowsAffected;
                        using (var command = new SqlCommand(deleteSql, connection, transaction))
                        {
                            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                            rowsAffected = command.ExecuteNonQuery();
                        }

                        if (rowsAffected > 0)
                        {
                            InsertAuditoria(connection, transaction, id, "Eliminar");
                        }

                        transaction.Commit();
                        return rowsAffected > 0;
                    }
                    catch
                    {
                        RollbackSafely(transaction);
                        throw;
                    }
                }
            }
        }

        private static void InsertAuditoria(SqlConnection connection, SqlTransaction transaction, int pruebaId, string operacion)
        {
            const string sql = @"
INSERT INTO dbo.PruebaAuditoria (PruebaId, Operacion, FechaUtc)
VALUES (@PruebaId, @Operacion, SYSUTCDATETIME());";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@PruebaId", SqlDbType.Int).Value = pruebaId;
                command.Parameters.Add("@Operacion", SqlDbType.VarChar, 20).Value = operacion;
                command.ExecuteNonQuery();
            }
        }

        private static bool IsUniqueNombreActivoViolation(SqlException sqlException)
        {
            return sqlException.Number == 2601
                && sqlException.Message.IndexOf(UniqueIndexName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void RollbackSafely(SqlTransaction transaction)
        {
            try
            {
                transaction.Rollback();
            }
            catch (Exception)
            {
                // La conexión pudo haberse cerrado/roto antes del rollback; no hay más que hacer aquí,
                // la excepción original sigue propagándose desde el bloque catch que la invocó.
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
