using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Repository
{
    public class RequestRepository : IRequestRepository
    {
        private readonly string _connectionString =
            System.Configuration.ConfigurationManager.ConnectionStrings["BoardRent"]?.ConnectionString ?? string.Empty;

        private const string BaseSelectSql =
            "SELECT r.*, ru.display_name AS renter_display_name, ou.display_name AS owner_display_name, " +
            "g.name AS game_name, g.image AS game_image, " +
            "ofu.display_name AS offering_user_display_name " +
            "FROM Requests r " +
            "LEFT JOIN Users ru ON ru.id = r.renter_id " +
            "LEFT JOIN Users ou ON ou.id = r.owner_id " +
            "LEFT JOIN Games g ON g.game_id = r.game_id " +
            "LEFT JOIN Users ofu ON ofu.id = r.offering_user_id";

        private static Request ReadFullRequestFromReader(SqlDataReader reader)
        {
            var game = new Game
            {
                Id = (int)reader["game_id"],
                Name = reader["game_name"] as string ?? string.Empty,
                Image = reader["game_image"] as byte[] ?? Array.Empty<byte>()
            };
            var renter = new User((int)reader["renter_id"], reader["renter_display_name"] as string ?? string.Empty);
            var owner = new User((int)reader["owner_id"], reader["owner_display_name"] as string ?? string.Empty);
            var status = (RequestStatus)(int)reader["status"];
            User? offeringUser = null;
            var offeringUserId = reader["offering_user_id"];
            if (offeringUserId != DBNull.Value)
            {
                offeringUser = new User((int)offeringUserId, reader["offering_user_display_name"] as string ?? string.Empty);
            }
            return new Request((int)reader["request_id"], game, renter, owner,
                (DateTime)reader["start_date"], (DateTime)reader["end_date"], status, offeringUser);
        }

        public ImmutableList<Request> GetAll()
        {
            var list = new List<Request>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectSql;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(ReadFullRequestFromReader(reader));
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public void Add(Request entity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    Add(entity, connection, transaction);
                    transaction.Commit();
                }
            }
        }

        public void Add(Request entity, SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "INSERT INTO Requests(game_id, renter_id, owner_id, start_date, end_date, status, offering_user_id) VALUES(@game_id, @renter_id, @owner_id, @start_date, @end_date, @status, @offering_user_id); SELECT SCOPE_IDENTITY();";
                command.Parameters.AddWithValue("@game_id", entity.Game?.Id ?? 0);
                command.Parameters.AddWithValue("@renter_id", entity.Renter?.Id ?? 0);
                command.Parameters.AddWithValue("@owner_id", entity.Owner?.Id ?? 0);
                command.Parameters.AddWithValue("@start_date", entity.StartDate);
                command.Parameters.AddWithValue("@end_date", entity.EndDate);
                command.Parameters.AddWithValue("@status", (int)entity.Status);
                command.Parameters.AddWithValue("@offering_user_id", entity.OfferingUser?.Id ?? (object)DBNull.Value);
                var newId = Convert.ToInt32(command.ExecuteScalar());
                entity.Id = newId;
            }
        }

        private static readonly string DeleteWithOutputSql =
            "DELETE r OUTPUT deleted.request_id, deleted.game_id, deleted.renter_id, deleted.owner_id, " +
            "deleted.start_date, deleted.end_date, deleted.status, deleted.offering_user_id, " +
            "ru.display_name AS renter_display_name, ou.display_name AS owner_display_name, " +
            "g.name AS game_name, g.image AS game_image, " +
            "ofu.display_name AS offering_user_display_name " +
            "FROM Requests r " +
            "LEFT JOIN Users ru ON ru.id = r.renter_id " +
            "LEFT JOIN Users ou ON ou.id = r.owner_id " +
            "LEFT JOIN Games g ON g.game_id = r.game_id " +
            "LEFT JOIN Users ofu ON ofu.id = r.offering_user_id " +
            "WHERE r.request_id = @id";

        public Request Delete(int removedEntityId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = DeleteWithOutputSql;
                    command.Parameters.AddWithValue("@id", removedEntityId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return ReadRequestFromDeleteReader(reader);
                    }
                }
            }
            throw new KeyNotFoundException();
        }

        public Request Delete(int id, SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = DeleteWithOutputSql;
                command.Parameters.AddWithValue("@id", id);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                        return ReadRequestFromDeleteReader(reader);
                }
            }
            throw new KeyNotFoundException();
        }

        private static Request ReadRequestFromDeleteReader(SqlDataReader reader)
        {
            var game = new Game
            {
                Id = (int)reader["game_id"],
                Name = reader["game_name"] as string ?? string.Empty,
                Image = reader["game_image"] as byte[] ?? Array.Empty<byte>()
            };
            var renter = new User((int)reader["renter_id"], reader["renter_display_name"] as string ?? string.Empty);
            var owner = new User((int)reader["owner_id"], reader["owner_display_name"] as string ?? string.Empty);
            var status = (RequestStatus)(int)reader["status"];
            User? offeringUser = null;
            var offeringUserId = reader["offering_user_id"];
            if (offeringUserId != DBNull.Value)
            {
                offeringUser = new User((int)offeringUserId, reader["offering_user_display_name"] as string ?? string.Empty);
            }
            return new Request((int)reader["request_id"], game, renter, owner,
                (DateTime)reader["start_date"], (DateTime)reader["end_date"], status, offeringUser);
        }

        public void Update(int updatedEntityId, Request newEntity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Requests SET game_id = @game_id, renter_id = @renter_id, owner_id = @owner_id, start_date = @start_date, end_date = @end_date, status = @status, offering_user_id = @offering_user_id WHERE request_id = @id";
                    command.Parameters.AddWithValue("@id", updatedEntityId);
                    command.Parameters.AddWithValue("@game_id", newEntity.Game?.Id ?? 0);
                    command.Parameters.AddWithValue("@renter_id", newEntity.Renter?.Id ?? 0);
                    command.Parameters.AddWithValue("@owner_id", newEntity.Owner?.Id ?? 0);
                    command.Parameters.AddWithValue("@start_date", newEntity.StartDate);
                    command.Parameters.AddWithValue("@end_date", newEntity.EndDate);
                    command.Parameters.AddWithValue("@status", (int)newEntity.Status);
                    command.Parameters.AddWithValue("@offering_user_id", newEntity.OfferingUser?.Id ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateStatus(int requestId, RequestStatus status, int? offeringUserId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Requests SET status = @status, offering_user_id = @offering_user_id WHERE request_id = @id";
                    command.Parameters.AddWithValue("@id", requestId);
                    command.Parameters.AddWithValue("@status", (int)status);
                    command.Parameters.AddWithValue("@offering_user_id", offeringUserId ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public Request Get(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectSql + " WHERE r.request_id = @id";
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadFullRequestFromReader(reader);
                        }
                    }
                }
            }
            throw new KeyNotFoundException();
        }

        public ImmutableList<Request> GetRequestsByOwner(int ownerId)
        {
            var list = new List<Request>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectSql + " WHERE r.owner_id = @owner_id";
                    command.Parameters.AddWithValue("@owner_id", ownerId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(ReadFullRequestFromReader(reader));
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public ImmutableList<Request> GetRequestsByRenter(int renterId)
        {
            var list = new List<Request>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectSql + " WHERE r.renter_id = @renter_id";
                    command.Parameters.AddWithValue("@renter_id", renterId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(ReadFullRequestFromReader(reader));
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public ImmutableList<Request> GetRequestsByGame(int gameId)
        {
            var list = new List<Request>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectSql + " WHERE r.game_id = @game_id";
                    command.Parameters.AddWithValue("@game_id", gameId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(ReadFullRequestFromReader(reader));
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }
    }
}
