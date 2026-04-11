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

        private const int BufferHours = 48;

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
                            list.Add(ReadFullRequestFromReader(reader));
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
                    AddInternal(entity, connection, transaction);
                    transaction.Commit();
                }
            }
        }

        private static void AddInternal(Request entity, SqlConnection connection, SqlTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "INSERT INTO Requests(game_id, renter_id, owner_id, start_date, end_date, status, offering_user_id) " +
                "VALUES(@game_id, @renter_id, @owner_id, @start_date, @end_date, @status, @offering_user_id); " +
                "SELECT SCOPE_IDENTITY();";
            command.Parameters.AddWithValue("@game_id", entity.Game?.Id ?? 0);
            command.Parameters.AddWithValue("@renter_id", entity.Renter?.Id ?? 0);
            command.Parameters.AddWithValue("@owner_id", entity.Owner?.Id ?? 0);
            command.Parameters.AddWithValue("@start_date", entity.StartDate);
            command.Parameters.AddWithValue("@end_date", entity.EndDate);
            command.Parameters.AddWithValue("@status", (int)entity.Status);
            command.Parameters.AddWithValue("@offering_user_id", entity.OfferingUser?.Id ?? (object)DBNull.Value);
            entity.Id = Convert.ToInt32(command.ExecuteScalar());
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
                offeringUser = new User((int)offeringUserId, reader["offering_user_display_name"] as string ?? string.Empty);
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
                    command.CommandText =
                        "UPDATE Requests SET game_id = @game_id, renter_id = @renter_id, owner_id = @owner_id, " +
                        "start_date = @start_date, end_date = @end_date, status = @status, " +
                        "offering_user_id = @offering_user_id WHERE request_id = @id";
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
                    command.CommandText =
                        "UPDATE Requests SET status = @status, offering_user_id = @offering_user_id WHERE request_id = @id";
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
                            return ReadFullRequestFromReader(reader);
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
                            list.Add(ReadFullRequestFromReader(reader));
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
                            list.Add(ReadFullRequestFromReader(reader));
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
                            list.Add(ReadFullRequestFromReader(reader));
                    }
                }
            }
            return list.ToImmutableList();
        }

        public (int RentalId, ImmutableList<Request> OverlappingRequests) ApproveAtomically(
            Request approvedRequest, DateTime bufferedStart, DateTime bufferedEnd)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                // 1. Find open requests that overlap with the approved request's buffered window
                var overlapping = QueryOverlappingRequests(
                    approvedRequest.Game?.Id ?? 0, approvedRequest.Id,
                    bufferedStart, bufferedEnd, connection, transaction);

                // 2. Delete notifications for all affected requests before deleting the requests
                //    (FK constraint: notifications reference requests)
                foreach (var req in overlapping)
                    DeleteNotificationsForRequest(req.Id, connection, transaction);
                DeleteNotificationsForRequest(approvedRequest.Id, connection, transaction);

                // 3. Insert the rental
                var rental = new Rental(
                    id: 0,
                    game: approvedRequest.Game,
                    renter: approvedRequest.Renter,
                    owner: approvedRequest.Owner,
                    startDate: approvedRequest.StartDate,
                    endDate: approvedRequest.EndDate);

                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText =
                        "INSERT INTO Rentals(game_id, renter_id, owner_id, start_date, end_date) " +
                        "VALUES(@game_id, @renter_id, @owner_id, @start_date, @end_date); SELECT SCOPE_IDENTITY();";
                    cmd.Parameters.AddWithValue("@game_id", rental.Game?.Id ?? 0);
                    cmd.Parameters.AddWithValue("@renter_id", rental.Renter?.Id ?? 0);
                    cmd.Parameters.AddWithValue("@owner_id", rental.Owner?.Id ?? 0);
                    cmd.Parameters.AddWithValue("@start_date", rental.StartDate);
                    cmd.Parameters.AddWithValue("@end_date", rental.EndDate);
                    rental.Id = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 4. Delete overlapping requests
                foreach (var req in overlapping)
                    DeleteRequestById(req.Id, connection, transaction);

                // 5. Delete the approved request
                DeleteRequestById(approvedRequest.Id, connection, transaction);

                transaction.Commit();
                return (rental.Id, overlapping.ToImmutableList());
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static List<Request> QueryOverlappingRequests(
            int gameId, int excludeRequestId,
            DateTime bufferedStart, DateTime bufferedEnd,
            SqlConnection connection, SqlTransaction transaction)
        {
            var list = new List<Request>();
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText =
                "SELECT r.request_id, r.game_id, r.renter_id, r.owner_id, r.start_date, r.end_date, " +
                "ru.display_name AS renter_display_name, ou.display_name AS owner_display_name, " +
                "g.name AS game_name, g.image AS game_image " +
                "FROM Requests r " +
                "LEFT JOIN Users ru ON ru.id = r.renter_id " +
                "LEFT JOIN Users ou ON ou.id = r.owner_id " +
                "LEFT JOIN Games g ON g.game_id = r.game_id " +
                "WHERE r.game_id = @game_id AND r.request_id != @exclude_id " +
                "AND r.start_date < @buffered_end AND r.end_date > @buffered_start";
            cmd.Parameters.AddWithValue("@game_id", gameId);
            cmd.Parameters.AddWithValue("@exclude_id", excludeRequestId);
            cmd.Parameters.AddWithValue("@buffered_end", bufferedEnd);
            cmd.Parameters.AddWithValue("@buffered_start", bufferedStart);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var game = new Game
                {
                    Id = (int)reader["game_id"],
                    Name = reader["game_name"] as string ?? string.Empty,
                    Image = reader["game_image"] as byte[] ?? Array.Empty<byte>()
                };
                var renter = new User((int)reader["renter_id"], reader["renter_display_name"] as string ?? string.Empty);
                var owner = new User((int)reader["owner_id"], reader["owner_display_name"] as string ?? string.Empty);
                list.Add(new Request((int)reader["request_id"], game, renter, owner,
                    (DateTime)reader["start_date"], (DateTime)reader["end_date"]));
            }
            return list;
        }

        private static void DeleteNotificationsForRequest(int requestId, SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = "DELETE FROM Notifications WHERE related_request_id = @id";
            cmd.Parameters.AddWithValue("@id", requestId);
            cmd.ExecuteNonQuery();
        }

        private static void DeleteRequestById(int requestId, SqlConnection connection, SqlTransaction transaction)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = "DELETE FROM Requests WHERE request_id = @id";
            cmd.Parameters.AddWithValue("@id", requestId);
            cmd.ExecuteNonQuery();
        }
    }
}
