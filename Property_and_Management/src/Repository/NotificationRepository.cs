using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;

namespace Property_and_Management.src.Repository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly string _connectionString =
            System.Configuration.ConfigurationManager.ConnectionStrings["BoardRent"]?.ConnectionString ?? string.Empty;

        private const string BaseSelectSql =
            "SELECT n.*, u.display_name AS user_display_name FROM Notifications n LEFT JOIN Users u ON u.id = n.user_id";

        private static Notification ReadNotificationFromReader(SqlDataReader reader)
        {
            var user = new User((int)reader["user_id"], reader["user_display_name"] as string ?? string.Empty);
            var type = (NotificationType)(int)reader["type"];
            var relatedRequestId = reader["related_request_id"];
            return new Notification(
                (int)reader["notification_id"], user,
                (DateTime)reader["timestamp"], (string)reader["title"], (string)reader["body"],
                type, relatedRequestId == DBNull.Value ? null : (int)relatedRequestId);
        }

        public ImmutableList<Notification> GetAll()
        {
            var list = new List<Notification>();
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
                            list.Add(ReadNotificationFromReader(reader));
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public void Add(Notification newEntity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Notifications(user_id, timestamp, title, body, type, related_request_id) VALUES(@user_id, @timestamp, @title, @body, @type, @related_request_id); SELECT SCOPE_IDENTITY();";
                    command.Parameters.AddWithValue("@user_id", newEntity.User?.Id ?? 0);
                    command.Parameters.AddWithValue("@timestamp", newEntity.Timestamp);
                    command.Parameters.AddWithValue("@title", newEntity.Title);
                    command.Parameters.AddWithValue("@body", newEntity.Body);
                    command.Parameters.AddWithValue("@type", (int)newEntity.Type);
                    command.Parameters.AddWithValue("@related_request_id", newEntity.RelatedRequestId ?? (object)DBNull.Value);
                    var newId = Convert.ToInt32(command.ExecuteScalar());
                    newEntity.Id = newId;
                }
            }
        }

        public Notification Delete(int removedEntityId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "DELETE n OUTPUT deleted.notification_id, deleted.user_id, deleted.timestamp, " +
                        "deleted.title, deleted.body, deleted.type, deleted.related_request_id, " +
                        "u.display_name AS user_display_name " +
                        "FROM Notifications n LEFT JOIN Users u ON u.id = n.user_id " +
                        "WHERE n.notification_id = @id";
                    command.Parameters.AddWithValue("@id", removedEntityId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadNotificationFromReader(reader);
                        }
                    }
                }
            }
            throw new KeyNotFoundException();
        }

        public void Update(int updatedEntityId, Notification newEntity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Notifications SET user_id = @user_id, timestamp = @timestamp, title = @title, body = @body, type = @type, related_request_id = @related_request_id WHERE notification_id = @id";
                    command.Parameters.AddWithValue("@id", updatedEntityId);
                    command.Parameters.AddWithValue("@user_id", newEntity.User?.Id ?? 0);
                    command.Parameters.AddWithValue("@timestamp", newEntity.Timestamp);
                    command.Parameters.AddWithValue("@title", newEntity.Title);
                    command.Parameters.AddWithValue("@body", newEntity.Body);
                    command.Parameters.AddWithValue("@type", (int)newEntity.Type);
                    command.Parameters.AddWithValue("@related_request_id", newEntity.RelatedRequestId ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public Notification Get(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectSql + " WHERE n.notification_id = @id";
                    command.Parameters.AddWithValue("@id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadNotificationFromReader(reader);
                        }
                    }
                }
            }
            throw new KeyNotFoundException();
        }

        public ImmutableList<Notification> GetNotificationsByUser(int userId)
        {
            var list = new List<Notification>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectSql + " WHERE n.user_id = @user_id";
                    command.Parameters.AddWithValue("@user_id", userId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(ReadNotificationFromReader(reader));
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public ImmutableList<Notification> GetActionableByRequestId(int requestId)
        {
            var list = new List<Notification>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectSql + " WHERE n.related_request_id = @request_id AND n.type = @type";
                    command.Parameters.AddWithValue("@request_id", requestId);
                    command.Parameters.AddWithValue("@type", (int)NotificationType.OfferReceived);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(ReadNotificationFromReader(reader));
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public void DeleteByRequestId(int requestId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Notifications WHERE related_request_id = @request_id";
                    command.Parameters.AddWithValue("@request_id", requestId);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
