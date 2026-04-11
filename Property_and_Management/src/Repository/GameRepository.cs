using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Repository
{
    public class GameRepository : IGameRepository
    {
        private const int MissingForeignKeyId = 0;
        private const int VarBinaryMaxLength = -1;

        private readonly string _connectionString =
            System.Configuration.ConfigurationManager.ConnectionStrings["BoardRent"]?.ConnectionString ?? string.Empty;

        public ImmutableList<Game> GetAll()
        {
            var list = new List<Game>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT g.*, u.display_name AS owner_display_name FROM Games g LEFT JOIN Users u ON u.id = g.owner_id";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var ownerDisplayName = reader["owner_display_name"] as string ?? string.Empty;
                            var owner = new User((int)reader["owner_id"], ownerDisplayName);
                            var game = new Game((int)reader["game_id"], owner, (string)reader["name"], Convert.ToDecimal(reader["price"]), (int)reader["minimum_player_number"], (int)reader["maximum_player_number"], (string)reader["description"], reader["image"] as byte[], Convert.ToBoolean(reader["is_active"]));
                            list.Add(game);
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public void Add(Game newEntity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Games(owner_id, name, price, minimum_player_number, maximum_player_number, description, image, is_active) VALUES(@owner_id, @name, @price, @min_players, @max_players, @description, @image, @is_active); SELECT SCOPE_IDENTITY();";
                    command.Parameters.AddWithValue("@owner_id", newEntity.Owner?.Id ?? MissingForeignKeyId);
                    command.Parameters.AddWithValue("@name", newEntity.Name ?? string.Empty);
                    command.Parameters.AddWithValue("@price", newEntity.Price);
                    command.Parameters.AddWithValue("@min_players", newEntity.MinimumPlayerNumber);
                    command.Parameters.AddWithValue("@max_players", newEntity.MaximumPlayerNumber);
                    command.Parameters.AddWithValue("@description", newEntity.Description ?? string.Empty);
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@image", System.Data.SqlDbType.VarBinary, VarBinaryMaxLength)
                    {
                        Value = (object)newEntity.Image ?? DBNull.Value
                    });
                    command.Parameters.AddWithValue("@is_active", newEntity.IsActive);

                    command.ExecuteNonQuery();
                }
            }
        }

        public ImmutableList<Game> GetGamesByOwner(int ownerId)
        {
            var list = new List<Game>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT g.*, u.display_name AS owner_display_name FROM Games g LEFT JOIN Users u ON u.id = g.owner_id WHERE g.owner_id = @owner_id";
                    command.Parameters.AddWithValue("@owner_id", ownerId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var ownerDisplayName = reader["owner_display_name"] as string ?? string.Empty;
                            var owner = new User((int)reader["owner_id"], ownerDisplayName);
                            var game = new Game((int)reader["game_id"], owner, (string)reader["name"], Convert.ToDecimal(reader["price"]), (int)reader["minimum_player_number"], (int)reader["maximum_player_number"], (string)reader["description"], reader["image"] as byte[], Convert.ToBoolean(reader["is_active"]));
                            list.Add(game);
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public void Update(int updatedEntityId, Game newEntity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Games SET owner_id = @owner_id, name = @name, price = @price, minimum_player_number = @min_players, maximum_player_number = @max_players, description = @description, image = @image, is_active = @is_active WHERE game_id = @id";
                    command.Parameters.AddWithValue("@id", updatedEntityId);
                    command.Parameters.AddWithValue("@owner_id", newEntity.Owner?.Id ?? MissingForeignKeyId);
                    command.Parameters.AddWithValue("@name", newEntity.Name ?? string.Empty);
                    command.Parameters.AddWithValue("@price", newEntity.Price);
                    command.Parameters.AddWithValue("@min_players", newEntity.MinimumPlayerNumber);
                    command.Parameters.AddWithValue("@max_players", newEntity.MaximumPlayerNumber);
                    command.Parameters.AddWithValue("@description", newEntity.Description ?? string.Empty);
                    command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@image", System.Data.SqlDbType.VarBinary, VarBinaryMaxLength)
                    {
                        Value = (object)newEntity.Image ?? DBNull.Value
                    });
                    command.Parameters.AddWithValue("@is_active", newEntity.IsActive);
                    command.ExecuteNonQuery();
                }
            }
        }

        public Game Get(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT g.*, u.display_name AS owner_display_name FROM Games g LEFT JOIN Users u ON u.id = g.owner_id WHERE g.game_id = @id";
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var ownerDisplayName = reader["owner_display_name"] as string ?? string.Empty;
                            var owner = new User((int)reader["owner_id"], ownerDisplayName);
                            return new Game((int)reader["game_id"], owner, (string)reader["name"], Convert.ToDecimal(reader["price"]), (int)reader["minimum_player_number"], (int)reader["maximum_player_number"], (string)reader["description"], reader["image"] as byte[], Convert.ToBoolean(reader["is_active"]));
                        }
                    }
                }
            }
            throw new KeyNotFoundException();
        }

        public Game Delete(int removedEntityId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "DELETE g OUTPUT deleted.game_id, deleted.owner_id, deleted.name, deleted.price, " +
                        "deleted.minimum_player_number, deleted.maximum_player_number, deleted.description, " +
                        "deleted.image, deleted.is_active, u.display_name AS owner_display_name " +
                        "FROM Games g LEFT JOIN Users u ON u.id = g.owner_id WHERE g.game_id = @id";
                    command.Parameters.AddWithValue("@id", removedEntityId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var owner = new User((int)reader["owner_id"], reader["owner_display_name"] as string ?? string.Empty);
                            return new Game((int)reader["game_id"], owner, (string)reader["name"],
                                Convert.ToDecimal(reader["price"]), (int)reader["minimum_player_number"],
                                (int)reader["maximum_player_number"], (string)reader["description"],
                                reader["image"] as byte[], Convert.ToBoolean(reader["is_active"]));
                        }
                    }
                }
            }
            throw new KeyNotFoundException();
        }
    }
}
