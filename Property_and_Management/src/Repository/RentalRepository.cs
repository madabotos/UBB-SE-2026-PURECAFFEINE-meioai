using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Repository
{
    public class RentalRepository : IRentalRepository
    {
        private readonly string _connectionString =
            System.Configuration.ConfigurationManager.ConnectionStrings["BoardRent"]?.ConnectionString ?? string.Empty;

        public ImmutableList<Rental> GetAll()
        {
            var list = new List<Rental>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT r.*, ru.display_name AS renter_display_name, ou.display_name AS owner_display_name, g.name AS game_name, g.image AS game_image FROM Rentals r LEFT JOIN Users ru ON ru.id = r.renter_id LEFT JOIN Users ou ON ou.id = r.owner_id LEFT JOIN Games g ON g.game_id = r.game_id";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var game = new Game
                            {
                                Id = (int)reader["game_id"],
                                Name = reader["game_name"] as string ?? string.Empty,
                                Image = reader["game_image"] as byte[] ?? Array.Empty<byte>()
                            };
                            var renterDisplayName = reader["renter_display_name"] as string ?? string.Empty;
                            var ownerDisplayName = reader["owner_display_name"] as string ?? string.Empty;
                            var renter = new User((int)reader["renter_id"], renterDisplayName);
                            var owner = new User((int)reader["owner_id"], ownerDisplayName);
                            var rent = new Rental((int)reader["rental_id"], game, renter, owner, (DateTime)reader["start_date"], (DateTime)reader["end_date"]);
                            list.Add(rent);
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public void Add(Rental entity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Rentals(game_id, renter_id, owner_id, start_date, end_date) VALUES(@game_id, @renter_id, @owner_id, @start_date, @end_date); SELECT SCOPE_IDENTITY();";
                    command.Parameters.AddWithValue("@game_id", entity.Game?.Id ?? 0);
                    command.Parameters.AddWithValue("@renter_id", entity.Renter?.Id ?? 0);
                    command.Parameters.AddWithValue("@owner_id", entity.Owner?.Id ?? 0);
                    command.Parameters.AddWithValue("@start_date", entity.StartDate);
                    command.Parameters.AddWithValue("@end_date", entity.EndDate);
                    var newId = Convert.ToInt32(command.ExecuteScalar());
                    entity.Id = newId;
                }
            }
        }

        public ImmutableList<Rental> GetRentalsByOwner(int ownerId)
        {
            var list = new List<Rental>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT r.*, ru.display_name AS renter_display_name, ou.display_name AS owner_display_name, g.name AS game_name, g.image AS game_image FROM Rentals r LEFT JOIN Users ru ON ru.id = r.renter_id LEFT JOIN Users ou ON ou.id = r.owner_id LEFT JOIN Games g ON g.game_id = r.game_id WHERE r.owner_id = @owner_id";
                    command.Parameters.AddWithValue("@owner_id", ownerId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var game = new Game
                            {
                                Id = (int)reader["game_id"],
                                Name = reader["game_name"] as string ?? string.Empty,
                                Image = reader["game_image"] as byte[] ?? Array.Empty<byte>()
                            };
                            var renterDisplayName = reader["renter_display_name"] as string ?? string.Empty;
                            var ownerDisplayName = reader["owner_display_name"] as string ?? string.Empty;
                            var renter = new User((int)reader["renter_id"], renterDisplayName);
                            var owner = new User((int)reader["owner_id"], ownerDisplayName);
                            var rent = new Rental((int)reader["rental_id"], game, renter, owner, (DateTime)reader["start_date"], (DateTime)reader["end_date"]);
                            list.Add(rent);
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public ImmutableList<Rental> GetRentalsByRenter(int renterId)
        {
            var list = new List<Rental>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT r.*, ru.display_name AS renter_display_name, ou.display_name AS owner_display_name, g.name AS game_name, g.image AS game_image FROM Rentals r LEFT JOIN Users ru ON ru.id = r.renter_id LEFT JOIN Users ou ON ou.id = r.owner_id LEFT JOIN Games g ON g.game_id = r.game_id WHERE r.renter_id = @renter_id";
                    command.Parameters.AddWithValue("@renter_id", renterId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var game = new Game
                            {
                                Id = (int)reader["game_id"],
                                Name = reader["game_name"] as string ?? string.Empty,
                                Image = reader["game_image"] as byte[] ?? Array.Empty<byte>()
                            };
                            var renterDisplayName = reader["renter_display_name"] as string ?? string.Empty;
                            var ownerDisplayName = reader["owner_display_name"] as string ?? string.Empty;
                            var renter = new User((int)reader["renter_id"], renterDisplayName);
                            var owner = new User((int)reader["owner_id"], ownerDisplayName);
                            var rent = new Rental((int)reader["rental_id"], game, renter, owner, (DateTime)reader["start_date"], (DateTime)reader["end_date"]);
                            list.Add(rent);
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public ImmutableList<Rental> GetRentalsByGame(int gameId)
        {
            var list = new List<Rental>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT r.*, ru.display_name AS renter_display_name, ou.display_name AS owner_display_name, g.name AS game_name, g.image AS game_image FROM Rentals r LEFT JOIN Users ru ON ru.id = r.renter_id LEFT JOIN Users ou ON ou.id = r.owner_id LEFT JOIN Games g ON g.game_id = r.game_id WHERE r.game_id = @game_id";
                    command.Parameters.AddWithValue("@game_id", gameId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var game = new Game
                            {
                                Id = (int)reader["game_id"],
                                Name = reader["game_name"] as string ?? string.Empty,
                                Image = reader["game_image"] as byte[] ?? Array.Empty<byte>()
                            };
                            var renterDisplayName = reader["renter_display_name"] as string ?? string.Empty;
                            var ownerDisplayName = reader["owner_display_name"] as string ?? string.Empty;
                            var renter = new User((int)reader["renter_id"], renterDisplayName);
                            var owner = new User((int)reader["owner_id"], ownerDisplayName);
                            var rent = new Rental((int)reader["rental_id"], game, renter, owner, (DateTime)reader["start_date"], (DateTime)reader["end_date"]);
                            list.Add(rent);
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public Rental Delete(int removedEntityId)
        {
            var entity = Get(removedEntityId);
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Rentals WHERE rental_id = @id";
                    command.Parameters.AddWithValue("@id", removedEntityId);
                    command.ExecuteNonQuery();
                }
            }
            return entity;
        }

        public void Update(int updatedEntityId, Rental newEntity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Rentals SET game_id = @game_id, renter_id = @renter_id, owner_id = @owner_id, start_date = @start_date, end_date = @end_date WHERE rental_id = @id";
                    command.Parameters.AddWithValue("@id", updatedEntityId);
                    command.Parameters.AddWithValue("@game_id", newEntity.Game?.Id ?? 0);
                    command.Parameters.AddWithValue("@renter_id", newEntity.Renter?.Id ?? 0);
                    command.Parameters.AddWithValue("@owner_id", newEntity.Owner?.Id ?? 0);
                    command.Parameters.AddWithValue("@start_date", newEntity.StartDate);
                    command.Parameters.AddWithValue("@end_date", newEntity.EndDate);
                    command.ExecuteNonQuery();
                }
            }
        }

        public Rental Get(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT r.*, ru.display_name AS renter_display_name, ou.display_name AS owner_display_name, g.name AS game_name, g.image AS game_image FROM Rentals r LEFT JOIN Users ru ON ru.id = r.renter_id LEFT JOIN Users ou ON ou.id = r.owner_id LEFT JOIN Games g ON g.game_id = r.game_id WHERE r.rental_id = @id";
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var game = new Game
                            {
                                Id = (int)reader["game_id"],
                                Name = reader["game_name"] as string ?? string.Empty,
                                Image = reader["game_image"] as byte[] ?? Array.Empty<byte>()
                            };
                            var renterDisplayName = reader["renter_display_name"] as string ?? string.Empty;
                            var ownerDisplayName = reader["owner_display_name"] as string ?? string.Empty;
                            var renter = new User((int)reader["renter_id"], renterDisplayName);
                            var owner = new User((int)reader["owner_id"], ownerDisplayName);
                            return new Rental((int)reader["rental_id"], game, renter, owner, (DateTime)reader["start_date"], (DateTime)reader["end_date"]);
                        }
                    }
                }
            }
            throw new KeyNotFoundException();
        }
    }
}
