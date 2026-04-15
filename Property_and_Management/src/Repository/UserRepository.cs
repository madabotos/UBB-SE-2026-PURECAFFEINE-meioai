using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Data.SqlClient;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly string connectionString =
            System.Configuration.ConfigurationManager.ConnectionStrings["BoardRent"]?.ConnectionString ?? string.Empty;

        public ImmutableList<User> GetAll()
        {
            var list = new List<User>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Users";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var userId = (int)reader["id"];
                            var displayName = reader["display_name"] as string ?? string.Empty;
                            list.Add(new User(userId, displayName));
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public void Add(User newEntity)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Users (display_name) VALUES (@display_name); SELECT SCOPE_IDENTITY();";
                    command.Parameters.AddWithValue("@display_name", newEntity.DisplayName ?? (object)DBNull.Value);
                    var newIdentifier = Convert.ToInt32(command.ExecuteScalar());
                    newEntity.Id = newIdentifier;
                }
            }
        }

        public User Delete(int removedEntityId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Users OUTPUT deleted.id, deleted.display_name WHERE id = @id";
                    command.Parameters.AddWithValue("@id", removedEntityId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User((int)reader["id"], reader["display_name"] as string ?? string.Empty);
                        }
                    }
                }
            }
            throw new KeyNotFoundException();
        }

        public void Update(int updatedEntityId, User newEntity)
        {
            if (updatedEntityId != newEntity.Id)
            {
                throw new ArgumentException("Id mismatch");
            }

            using (var connection = new SqlConnection(connectionString))
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
            using (var connection = new SqlConnection(connectionString))
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
