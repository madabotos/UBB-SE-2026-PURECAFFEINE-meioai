using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using Microsoft.Data.SqlClient;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Repository
{
    public class RequestRepository : IRequestRepository
    {
        private const int NewEntityId = 0;
        private const int MissingForeignKeyId = 0;
        private const string ConnectionStringName = "BoardRent";

        private readonly string connectionString =
            System.Configuration.ConfigurationManager.ConnectionStrings[ConnectionStringName]?.ConnectionString ?? string.Empty;

        private const string BaseSelectQuery =
            "SELECT r.*, renterUser.display_name AS renter_display_name, ownerUser.display_name AS owner_display_name, " +
            "g.name AS game_name, g.image AS game_image, " +
            "offeringUser.display_name AS offering_user_display_name " +
            "FROM Requests r " +
            "LEFT JOIN Users renterUser ON renterUser.Id = r.renter_id " +
            "LEFT JOIN Users ownerUser ON ownerUser.Id = r.owner_id " +
            "LEFT JOIN Games g ON g.game_id = r.game_id " +
            "LEFT JOIN Users offeringUser ON offeringUser.Id = r.offering_user_id";

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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectQuery;
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

        public void Add(Request request)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    AddInternal(request, connection, transaction);
                    transaction.Commit();
                }
            }
        }

        private static void AddInternal(Request request, SqlConnection connection, SqlTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "INSERT INTO Requests(game_id, renter_id, owner_id, start_date, end_date, status, offering_user_id) " +
                "VALUES(@game_id, @renter_id, @owner_id, @start_date, @end_date, @status, @offering_user_id); " +
                "SELECT SCOPE_IDENTITY();";
            command.Parameters.AddWithValue("@game_id", request.Game?.Id ?? MissingForeignKeyId);
            command.Parameters.AddWithValue("@renter_id", request.Renter?.Id ?? MissingForeignKeyId);
            command.Parameters.AddWithValue("@owner_id", request.Owner?.Id ?? MissingForeignKeyId);
            command.Parameters.AddWithValue("@start_date", request.StartDate);
            command.Parameters.AddWithValue("@end_date", request.EndDate);
            command.Parameters.AddWithValue("@status", (int)request.Status);
            command.Parameters.AddWithValue("@offering_user_id", request.OfferingUser?.Id ?? (object)DBNull.Value);
            request.Id = Convert.ToInt32(command.ExecuteScalar());
        }

        private static readonly string DeleteWithOutputQuery =
            "DELETE r OUTPUT deleted.request_id, deleted.game_id, deleted.renter_id, deleted.owner_id, " +
            "deleted.start_date, deleted.end_date, deleted.status, deleted.offering_user_id, " +
            "renterUser.display_name AS renter_display_name, ownerUser.display_name AS owner_display_name, " +
            "g.name AS game_name, g.image AS game_image, " +
            "offeringUser.display_name AS offering_user_display_name " +
            "FROM Requests r " +
            "LEFT JOIN Users renterUser ON renterUser.Id = r.renter_id " +
            "LEFT JOIN Users ownerUser ON ownerUser.Id = r.owner_id " +
            "LEFT JOIN Games g ON g.game_id = r.game_id " +
            "LEFT JOIN Users offeringUser ON offeringUser.Id = r.offering_user_id " +
            "WHERE r.request_id = @id";

        public Request Delete(int removedEntityId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = DeleteWithOutputQuery;
                    command.Parameters.AddWithValue("@id", removedEntityId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadRequestFromDeleteReader(reader);
                        }
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
            {
                offeringUser = new User((int)offeringUserId, reader["offering_user_display_name"] as string ?? string.Empty);
            }

            return new Request((int)reader["request_id"], game, renter, owner,
                (DateTime)reader["start_date"], (DateTime)reader["end_date"], status, offeringUser);
        }

        public void Update(int updatedEntityId, Request newEntity)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "UPDATE Requests SET game_id = @game_id, renter_id = @renter_id, owner_id = @owner_id, " +
                        "start_date = @start_date, end_date = @end_date, status = @status, " +
                        "offering_user_id = @offering_user_id WHERE request_id = @id";
                    command.Parameters.AddWithValue("@id", updatedEntityId);
                    command.Parameters.AddWithValue("@game_id", newEntity.Game?.Id ?? MissingForeignKeyId);
                    command.Parameters.AddWithValue("@renter_id", newEntity.Renter?.Id ?? MissingForeignKeyId);
                    command.Parameters.AddWithValue("@owner_id", newEntity.Owner?.Id ?? MissingForeignKeyId);
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
            using (var connection = new SqlConnection(connectionString))
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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectQuery + " WHERE r.request_id = @id";
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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectQuery + " WHERE r.owner_id = @owner_id";
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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectQuery + " WHERE r.renter_id = @renter_id";
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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectQuery + " WHERE r.game_id = @game_id";
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

        public ImmutableList<Request> GetOverlappingRequests(
            int gameId,
            int excludeRequestId,
            DateTime bufferedStartDate,
            DateTime bufferedEndDate)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            return QueryOverlappingRequests(
                gameId,
                excludeRequestId,
                bufferedStartDate,
                bufferedEndDate,
                connection,
                transaction: null).ToImmutableList();
        }

        public int ApproveAtomically(
            Request approvedRequest,
            ImmutableList<Request> overlappingRequests)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                foreach (var overlappingRequest in overlappingRequests)
                {
                    DeleteNotificationsForRequest(overlappingRequest.Id, connection, transaction);
                }

                DeleteNotificationsForRequest(approvedRequest.Id, connection, transaction);

                var newRentalIdentifier = InsertRental(approvedRequest, connection, transaction);

                foreach (var overlappingRequest in overlappingRequests)
                {
                    DeleteRequest(overlappingRequest.Id, connection, transaction);
                }

                DeleteRequest(approvedRequest.Id, connection, transaction);

                transaction.Commit();
                return newRentalIdentifier;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static int InsertRental(Request approvedRequest, SqlConnection connection, SqlTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "INSERT INTO Rentals(game_id, renter_id, owner_id, start_date, end_date) " +
                "VALUES(@game_id, @renter_id, @owner_id, @start_date, @end_date); SELECT SCOPE_IDENTITY();";
            command.Parameters.AddWithValue("@game_id", approvedRequest.Game?.Id ?? MissingForeignKeyId);
            command.Parameters.AddWithValue("@renter_id", approvedRequest.Renter?.Id ?? MissingForeignKeyId);
            command.Parameters.AddWithValue("@owner_id", approvedRequest.Owner?.Id ?? MissingForeignKeyId);
            command.Parameters.AddWithValue("@start_date", approvedRequest.StartDate);
            command.Parameters.AddWithValue("@end_date", approvedRequest.EndDate);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static List<Request> QueryOverlappingRequests(
            int gameId,
            int excludeRequestId,
            DateTime bufferedStartDate,
            DateTime bufferedEndDate,
            SqlConnection connection,
            SqlTransaction? transaction)
        {
            var list = new List<Request>();
            using var command = connection.CreateCommand();
            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            command.CommandText =
                "SELECT r.request_id, r.game_id, r.renter_id, r.owner_id, r.start_date, r.end_date, " +
                "renterUser.display_name AS renter_display_name, ownerUser.display_name AS owner_display_name, " +
                "g.name AS game_name, g.image AS game_image " +
                "FROM Requests r " +
                "LEFT JOIN Users renterUser ON renterUser.Id = r.renter_id " +
                "LEFT JOIN Users ownerUser ON ownerUser.Id = r.owner_id " +
                "LEFT JOIN Games g ON g.game_id = r.game_id " +
                "WHERE r.game_id = @game_id AND r.request_id != @exclude_id " +
                "AND r.start_date < @buffered_end AND r.end_date > @buffered_start";
            command.Parameters.AddWithValue("@game_id", gameId);
            command.Parameters.AddWithValue("@exclude_id", excludeRequestId);
            command.Parameters.AddWithValue("@buffered_end", bufferedEndDate);
            command.Parameters.AddWithValue("@buffered_start", bufferedStartDate);
            using var reader = command.ExecuteReader();
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
                list.Add(new Request(
                    (int)reader["request_id"],
                    game,
                    renter,
                    owner,
                    (DateTime)reader["start_date"],
                    (DateTime)reader["end_date"]));
            }

            return list;
        }

        private static void DeleteNotificationsForRequest(int requestId, SqlConnection connection, SqlTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Notifications WHERE related_request_id = @id";
            command.Parameters.AddWithValue("@id", requestId);
            command.ExecuteNonQuery();
        }

        private static void DeleteRequest(int requestId, SqlConnection connection, SqlTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Requests WHERE request_id = @id";
            command.Parameters.AddWithValue("@id", requestId);
            command.ExecuteNonQuery();
        }
    }
}