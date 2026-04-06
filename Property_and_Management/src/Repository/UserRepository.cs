using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.Linq;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString =
            System.Configuration.ConfigurationManager.ConnectionStrings["BoardRent"]?.ConnectionString ?? string.Empty;

        public ImmutableList<User> GetAll()
        {
            var list = new List<User>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Users";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var id = (int)reader["id"];
                            var displayName = reader["display_name"] as string ?? string.Empty;
                            list.Add(new User(id, displayName));
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public void Add(User newEntity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Users (display_name) VALUES (@display_name); SELECT SCOPE_IDENTITY();";
                    command.Parameters.AddWithValue("@display_name", newEntity.DisplayName ?? (object)DBNull.Value);
                    var newId = Convert.ToInt32(command.ExecuteScalar());
                    newEntity.Id = newId;
                }
            }
        }

        public User Delete(int removedEntityId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Users OUTPUT deleted.id, deleted.display_name WHERE id = @id";
                    command.Parameters.AddWithValue("@id", removedEntityId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            return new User((int)reader["id"], reader["display_name"] as string ?? string.Empty);
                    }
                }
            }
            throw new KeyNotFoundException();
        }

        public void Update(int updatedEntityId, User newEntity)
        {
            if (updatedEntityId != newEntity.Id)
                throw new ArgumentException("Id mismatch");

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Users SET display_name = @display_name WHERE id = @id";
                    command.Parameters.AddWithValue("@display_name", newEntity.DisplayName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@id", updatedEntityId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public User Get(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Users WHERE id = @id";
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var userId = (int)reader["id"];
                            var displayName = reader["display_name"] as string ?? string.Empty;
                            return new User(userId, displayName);
                        }
                    }
                }
            }

            throw new KeyNotFoundException();
        }
    }
}
