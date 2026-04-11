using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Repository
{
    public class RentalRepository : IRentalRepository
    {
        private const int MissingForeignKeyId = 0;
        private const int NoConflictsCount = 0;

        private readonly string _connectionString =
            System.Configuration.ConfigurationManager.ConnectionStrings["BoardRent"]?.ConnectionString ?? string.Empty;

        private const int BufferHours = 48;

        private const string SelectAllSql =
            "SELECT r.*, renterUser.display_name AS renter_display_name, ownerUser.display_name AS owner_display_name, " +
            "g.name AS game_name, g.image AS game_image " +
            "FROM Rentals r " +
            "LEFT JOIN Users renterUser ON renterUser.id = r.renter_id " +
            "LEFT JOIN Users ownerUser ON ownerUser.id = r.owner_id " +
            "LEFT JOIN Games g ON g.game_id = r.game_id";

        private static Rental ReadRentalFromReader(SqlDataReader reader)
        {
            var game = new Game
            {
                Identifier = (int)reader["game_id"],
                Name = reader["game_name"] as string ?? string.Empty,
                Image = reader["game_image"] as byte[] ?? Array.Empty<byte>()
            };
            var renter = new User((int)reader["renter_id"], reader["renter_display_name"] as string ?? string.Empty);
            var owner = new User((int)reader["owner_id"], reader["owner_display_name"] as string ?? string.Empty);
            return new Rental((int)reader["rental_id"], game, renter, owner,
                (DateTime)reader["start_date"], (DateTime)reader["end_date"]);
        }

        public ImmutableList<Rental> GetAll()
        {
            var list = new List<Rental>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = SelectAllSql;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(ReadRentalFromReader(reader));
                    }
                }
            }
            return list.ToImmutableList();
        }

        public void Add(Rental rental)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    AddInternal(rental, connection, transaction);
                    transaction.Commit();
                }
            }
        }

        private static void AddInternal(Rental rental, SqlConnection connection, SqlTransaction transaction)
        {
            using var command = connection.CreateCommand();
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

        public void AddConfirmed(Rental rental)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                if (!IsSlotAvailableInternal(rental.Game?.Identifier ?? MissingForeignKeyId, rental.StartDate, rental.EndDate, connection, transaction))
                    throw new InvalidOperationException(
                        $"Selected dates fall within the mandatory {BufferHours}-hour buffer of another rental.");

                AddInternal(rental, connection, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static bool IsSlotAvailableInternal(int gameIdentifier, DateTime newStart, DateTime newEnd,
            SqlConnection connection, SqlTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "SELECT COUNT(*) FROM Rentals " +
                "WHERE game_id = @game_id " +
                "AND @new_start < DATEADD(HOUR, @buffer, end_date) " +
                "AND @new_end > DATEADD(HOUR, -@buffer, start_date)";
            command.Parameters.AddWithValue("@game_id", gameIdentifier);
            command.Parameters.AddWithValue("@new_start", newStart);
            command.Parameters.AddWithValue("@new_end", newEnd);
            command.Parameters.AddWithValue("@buffer", BufferHours);
            return Convert.ToInt32(command.ExecuteScalar()) == NoConflictsCount;
        }

        public ImmutableList<Rental> GetRentalsByOwner(int ownerIdentifier)
        {
            var list = new List<Rental>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = SelectAllSql + " WHERE r.owner_id = @owner_id";
                    command.Parameters.AddWithValue("@owner_id", ownerIdentifier);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(ReadRentalFromReader(reader));
                    }
                }
            }
            return list.ToImmutableList();
        }

        public ImmutableList<Rental> GetRentalsByRenter(int renterIdentifier)
        {
            var list = new List<Rental>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = SelectAllSql + " WHERE r.renter_id = @renter_id";
                    command.Parameters.AddWithValue("@renter_id", renterIdentifier);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(ReadRentalFromReader(reader));
                    }
                }
            }
            return list.ToImmutableList();
        }

        public ImmutableList<Rental> GetRentalsByGame(int gameIdentifier)
        {
            var list = new List<Rental>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = SelectAllSql + " WHERE r.game_id = @game_id";
                    command.Parameters.AddWithValue("@game_id", gameIdentifier);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(ReadRentalFromReader(reader));
                    }
                }
            }
            return list.ToImmutableList();
        }

        public Rental Delete(int removedEntityIdentifier)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "DELETE r OUTPUT deleted.rental_id, deleted.game_id, deleted.renter_id, deleted.owner_id, " +
                        "deleted.start_date, deleted.end_date, " +
                        "renterUser.display_name AS renter_display_name, ownerUser.display_name AS owner_display_name, " +
                        "g.name AS game_name, g.image AS game_image " +
                        "FROM Rentals r " +
                        "LEFT JOIN Users renterUser ON renterUser.id = r.renter_id " +
                        "LEFT JOIN Users ownerUser ON ownerUser.id = r.owner_id " +
                        "LEFT JOIN Games g ON g.game_id = r.game_id " +
                        "WHERE r.rental_id = @id";
                    command.Parameters.AddWithValue("@id", removedEntityIdentifier);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return ReadRentalFromReader(reader);
                    }
                }
            }
            throw new KeyNotFoundException();
        }

        public void Update(int updatedEntityIdentifier, Rental newEntity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "UPDATE Rentals SET game_id = @game_id, renter_id = @renter_id, owner_id = @owner_id, " +
                        "start_date = @start_date, end_date = @end_date WHERE rental_id = @id";
                    command.Parameters.AddWithValue("@id", updatedEntityIdentifier);
                    command.Parameters.AddWithValue("@game_id", newEntity.Game?.Identifier ?? MissingForeignKeyId);
                    command.Parameters.AddWithValue("@renter_id", newEntity.Renter?.Identifier ?? MissingForeignKeyId);
                    command.Parameters.AddWithValue("@owner_id", newEntity.Owner?.Identifier ?? MissingForeignKeyId);
                    command.Parameters.AddWithValue("@start_date", newEntity.StartDate);
                    command.Parameters.AddWithValue("@end_date", newEntity.EndDate);
                    command.ExecuteNonQuery();
                }
            }
        }

        public Rental Get(int identifier)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = SelectAllSql + " WHERE r.rental_id = @id";
                    command.Parameters.AddWithValue("@id", identifier);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return ReadRentalFromReader(reader);
                    }
                }
            }
            throw new KeyNotFoundException();
        }
    }
}





