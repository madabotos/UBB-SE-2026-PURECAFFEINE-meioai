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
        private const int NewEntityIdentifier = 0;
        private const int MissingForeignKeyId = 0;

        private readonly string _connectionString =
            System.Configuration.ConfigurationManager.ConnectionStrings["BoardRent"]?.ConnectionString ?? string.Empty;

        private const int BufferHours = 48;

        private const string BaseSelectQuery =
            "SELECT r.*, renterUser.display_name AS renter_display_name, ownerUser.display_name AS owner_display_name, " +
            "g.name AS game_name, g.image AS game_image, " +
            "offeringUser.display_name AS offering_user_display_name " +
            "FROM Requests r " +
            "LEFT JOIN Users renterUser ON renterUser.id = r.renter_id " +
            "LEFT JOIN Users ownerUser ON ownerUser.id = r.owner_id " +
            "LEFT JOIN Games g ON g.game_id = r.game_id " +
            "LEFT JOIN Users offeringUser ON offeringUser.id = r.offering_user_id";

        private static Request ReadFullRequestFromReader(SqlDataReader reader)
        {
            var game = new Game
            {
                Identifier = (int)reader["game_id"],
                Name = reader["game_name"] as string ?? string.Empty,
                Image = reader["game_image"] as byte[] ?? Array.Empty<byte>()
            };
            var renter = new User((int)reader["renter_id"], reader["renter_display_name"] as string ?? string.Empty);
            var owner = new User((int)reader["owner_id"], reader["owner_display_name"] as string ?? string.Empty);
            var status = (RequestStatus)(int)reader["status"];
            User? offeringUser = null;
            var offeringUserIdentifier = reader["offering_user_id"];
            if (offeringUserIdentifier != DBNull.Value)
            {
                offeringUser = new User((int)offeringUserIdentifier, reader["offering_user_display_name"] as string ?? string.Empty);
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
                    command.CommandText = BaseSelectQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(ReadFullRequestFromReader(reader));
                    }
                }
            }
            return list.ToImmutableList();
        }

        public void Add(Request request)
        {
            using (var connection = new SqlConnection(_connectionString))
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
            command.Parameters.AddWithValue("@game_id", request.Game?.Identifier ?? MissingForeignKeyId);
            command.Parameters.AddWithValue("@renter_id", request.Renter?.Identifier ?? MissingForeignKeyId);
            command.Parameters.AddWithValue("@owner_id", request.Owner?.Identifier ?? MissingForeignKeyId);
            command.Parameters.AddWithValue("@start_date", request.StartDate);
            command.Parameters.AddWithValue("@end_date", request.EndDate);
            command.Parameters.AddWithValue("@status", (int)request.Status);
            command.Parameters.AddWithValue("@offering_user_id", request.OfferingUser?.Identifier ?? (object)DBNull.Value);
            request.Identifier = Convert.ToInt32(command.ExecuteScalar());
        }

        private static readonly string DeleteWithOutputQuery =
            "DELETE r OUTPUT deleted.request_id, deleted.game_id, deleted.renter_id, deleted.owner_id, " +
            "deleted.start_date, deleted.end_date, deleted.status, deleted.offering_user_id, " +
            "renterUser.display_name AS renter_display_name, ownerUser.display_name AS owner_display_name, " +
            "g.name AS game_name, g.image AS game_image, " +
            "offeringUser.display_name AS offering_user_display_name " +
            "FROM Requests r " +
            "LEFT JOIN Users renterUser ON renterUser.id = r.renter_id " +
            "LEFT JOIN Users ownerUser ON ownerUser.id = r.owner_id " +
            "LEFT JOIN Games g ON g.game_id = r.game_id " +
            "LEFT JOIN Users offeringUser ON offeringUser.id = r.offering_user_id " +
            "WHERE r.request_id = @id";

        public Request Delete(int removedEntityIdentifier)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = DeleteWithOutputQuery;
                    command.Parameters.AddWithValue("@id", removedEntityIdentifier);
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
                Identifier = (int)reader["game_id"],
                Name = reader["game_name"] as string ?? string.Empty,
                Image = reader["game_image"] as byte[] ?? Array.Empty<byte>()
            };
            var renter = new User((int)reader["renter_id"], reader["renter_display_name"] as string ?? string.Empty);
            var owner = new User((int)reader["owner_id"], reader["owner_display_name"] as string ?? string.Empty);
            var status = (RequestStatus)(int)reader["status"];
            User? offeringUser = null;
            var offeringUserIdentifier = reader["offering_user_id"];
            if (offeringUserIdentifier != DBNull.Value)
                offeringUser = new User((int)offeringUserIdentifier, reader["offering_user_display_name"] as string ?? string.Empty);
            return new Request((int)reader["request_id"], game, renter, owner,
                (DateTime)reader["start_date"], (DateTime)reader["end_date"], status, offeringUser);
        }

        public void Update(int updatedEntityIdentifier, Request newEntity)
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
                    command.Parameters.AddWithValue("@id", updatedEntityIdentifier);
                    command.Parameters.AddWithValue("@game_id", newEntity.Game?.Identifier ?? MissingForeignKeyId);
                    command.Parameters.AddWithValue("@renter_id", newEntity.Renter?.Identifier ?? MissingForeignKeyId);
                    command.Parameters.AddWithValue("@owner_id", newEntity.Owner?.Identifier ?? MissingForeignKeyId);
                    command.Parameters.AddWithValue("@start_date", newEntity.StartDate);
                    command.Parameters.AddWithValue("@end_date", newEntity.EndDate);
                    command.Parameters.AddWithValue("@status", (int)newEntity.Status);
                    command.Parameters.AddWithValue("@offering_user_id", newEntity.OfferingUser?.Identifier ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateStatus(int requestIdentifier, RequestStatus status, int? offeringUserIdentifier)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "UPDATE Requests SET status = @status, offering_user_id = @offering_user_id WHERE request_id = @id";
                    command.Parameters.AddWithValue("@id", requestIdentifier);
                    command.Parameters.AddWithValue("@status", (int)status);
                    command.Parameters.AddWithValue("@offering_user_id", offeringUserIdentifier ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public Request Get(int identifier)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectQuery + " WHERE r.request_id = @id";
                    command.Parameters.AddWithValue("@id", identifier);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return ReadFullRequestFromReader(reader);
                    }
                }
            }
            throw new KeyNotFoundException();
        }

        public ImmutableList<Request> GetRequestsByOwner(int ownerIdentifier)
        {
            var list = new List<Request>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectQuery + " WHERE r.owner_id = @owner_id";
                    command.Parameters.AddWithValue("@owner_id", ownerIdentifier);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(ReadFullRequestFromReader(reader));
                    }
                }
            }
            return list.ToImmutableList();
        }

        public ImmutableList<Request> GetRequestsByRenter(int renterIdentifier)
        {
            var list = new List<Request>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectQuery + " WHERE r.renter_id = @renter_id";
                    command.Parameters.AddWithValue("@renter_id", renterIdentifier);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(ReadFullRequestFromReader(reader));
                    }
                }
            }
            return list.ToImmutableList();
        }

        public ImmutableList<Request> GetRequestsByGame(int gameIdentifier)
        {
            var list = new List<Request>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectQuery + " WHERE r.game_id = @game_id";
                    command.Parameters.AddWithValue("@game_id", gameIdentifier);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(ReadFullRequestFromReader(reader));
                    }
                }
            }
            return list.ToImmutableList();
        }

        public (int rentalIdentifier, ImmutableList<Request> OverlappingRequests) ApproveAtomically(
            Request approvedRequest, DateTime bufferedStart, DateTime bufferedEnd)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                // 1. Find open requests that overlap with the approved request's buffered window
                var overlappingRequests = QueryOverlappingRequests(
                    approvedRequest.Game?.Identifier ?? MissingForeignKeyId, approvedRequest.Identifier,
                    bufferedStart, bufferedEnd, connection, transaction);

                // 2. Delete notifications for all affected requests before deleting the requests
                //    (FK constraint: notifications reference requests)
                foreach (var overlappingRequest in overlappingRequests)
                    DeleteNotificationsForRequest(overlappingRequest.Identifier, connection, transaction);
                DeleteNotificationsForRequest(approvedRequest.Identifier, connection, transaction);

                // 3. Insert the rental
                var rental = new Rental(
                    identifier: NewEntityIdentifier,
                    game: approvedRequest.Game,
                    renter: approvedRequest.Renter,
                    owner: approvedRequest.Owner,
                    startDate: approvedRequest.StartDate,
                    endDate: approvedRequest.EndDate);

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText =
                        "INSERT INTO Rentals(game_id, renter_id, owner_id, start_date, end_date) " +
                        "VALUES(@game_id, @renter_id, @owner_id, @start_date, @end_date); SELECT SCOPE_IDENTITY();";
                    command.Parameters.AddWithValue("@game_id", rental.Game?.Identifier ?? MissingForeignKeyId);
                    command.Parameters.AddWithValue("@renter_id", rental.Renter?.Identifier ?? MissingForeignKeyId);
                    command.Parameters.AddWithValue("@owner_id", rental.Owner?.Identifier ?? MissingForeignKeyId);
                    command.Parameters.AddWithValue("@start_date", rental.StartDate);
                    command.Parameters.AddWithValue("@end_date", rental.EndDate);
                    rental.Identifier = Convert.ToInt32(command.ExecuteScalar());
                }

                // 4. Delete overlapping requests
                foreach (var overlappingRequest in overlappingRequests)
                    DeleteRequestByIdentifier(overlappingRequest.Identifier, connection, transaction);

                // 5. Delete the approved request
                DeleteRequestByIdentifier(approvedRequest.Identifier, connection, transaction);

                transaction.Commit();
                return (rental.Identifier, overlappingRequests.ToImmutableList());
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static List<Request> QueryOverlappingRequests(
            int gameIdentifier, int excludeRequestId,
            DateTime bufferedStart, DateTime bufferedEnd,
            SqlConnection connection, SqlTransaction transaction)
        {
            var list = new List<Request>();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "SELECT r.request_id, r.game_id, r.renter_id, r.owner_id, r.start_date, r.end_date, " +
                "renterUser.display_name AS renter_display_name, ownerUser.display_name AS owner_display_name, " +
                "g.name AS game_name, g.image AS game_image " +
                "FROM Requests r " +
                "LEFT JOIN Users renterUser ON renterUser.id = r.renter_id " +
                "LEFT JOIN Users ownerUser ON ownerUser.id = r.owner_id " +
                "LEFT JOIN Games g ON g.game_id = r.game_id " +
                "WHERE r.game_id = @game_id AND r.request_id != @exclude_id " +
                "AND r.start_date < @buffered_end AND r.end_date > @buffered_start";
            command.Parameters.AddWithValue("@game_id", gameIdentifier);
            command.Parameters.AddWithValue("@exclude_id", excludeRequestId);
            command.Parameters.AddWithValue("@buffered_end", bufferedEnd);
            command.Parameters.AddWithValue("@buffered_start", bufferedStart);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var game = new Game
                {
                    Identifier = (int)reader["game_id"],
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

        private static void DeleteNotificationsForRequest(int requestIdentifier, SqlConnection connection, SqlTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Notifications WHERE related_request_id = @id";
            command.Parameters.AddWithValue("@id", requestIdentifier);
            command.ExecuteNonQuery();
        }

        private static void DeleteRequestByIdentifier(int requestIdentifier, SqlConnection connection, SqlTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Requests WHERE request_id = @id";
            command.Parameters.AddWithValue("@id", requestIdentifier);
            command.ExecuteNonQuery();
        }
    }
}





