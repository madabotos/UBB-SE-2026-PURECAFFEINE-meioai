using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Data.SqlClient;
using Property_and_Management.Src.Interface;
using Property_and_Management.Src.Model;

namespace Property_and_Management.Src.Repository
{
    public class NotificationRepository : INotificationRepository
    {
        private const int MissingUserId = 0;

        private readonly string connectionString =
            System.Configuration.ConfigurationManager.ConnectionStrings["BoardRent"]?.ConnectionString ?? string.Empty;

        private const string BaseSelectQuery =
            "SELECT n.*, u.display_name AS user_display_name FROM Notifications n LEFT JOIN Users u ON u.id = n.user_id";

        private static Notification ReadNotificationFromReader(SqlDataReader reader)
        {
            var user = new User((int)reader["user_id"], reader["user_display_name"] as string ?? string.Empty);
            var type = (NotificationType)(int)reader["type"];
            var relatedRequestIdValue = reader["related_request_id"];
            return new Notification(
                (int)reader["notification_id"], user,
                (DateTime)reader["timestamp"], (string)reader["title"], (string)reader["body"],
                type, relatedRequestIdValue == DBNull.Value ? null : (int)relatedRequestIdValue);
        }

        public ImmutableList<Notification> GetAll()
        {
            var list = new List<Notification>();
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
                            list.Add(ReadNotificationFromReader(reader));
                        }
                    }
                }
            }
            return list.ToImmutableList();
        }

        public void Add(Notification newEntity)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Notifications(user_id, timestamp, title, body, type, related_request_id) VALUES(@user_id, @timestamp, @title, @body, @type, @related_request_id); SELECT SCOPE_IDENTITY();";
                    command.Parameters.AddWithValue("@user_id", newEntity.User?.Id ?? MissingUserId);
                    command.Parameters.AddWithValue("@timestamp", newEntity.Timestamp);
                    command.Parameters.AddWithValue("@title", newEntity.Title);
                    command.Parameters.AddWithValue("@body", newEntity.Body);
                    command.Parameters.AddWithValue("@type", (int)newEntity.Type);
                    command.Parameters.AddWithValue("@related_request_id", newEntity.RelatedRequestId ?? (object)DBNull.Value);
                    var newIdentifier = Convert.ToInt32(command.ExecuteScalar());
                    newEntity.Id = newIdentifier;
                }
            }
        }

        public Notification Delete(int removedEntityId)
        {
            using (var connection = new SqlConnection(connectionString))
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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Notifications SET user_id = @user_id, timestamp = @timestamp, title = @title, body = @body, type = @type, related_request_id = @related_request_id WHERE notification_id = @id";
                    command.Parameters.AddWithValue("@id", updatedEntityId);
                    command.Parameters.AddWithValue("@user_id", newEntity.User?.Id ?? MissingUserId);
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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectQuery + " WHERE n.notification_id = @id";
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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = BaseSelectQuery + " WHERE n.user_id = @user_id";
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

        public void DeleteNotificationsLinkedToRequest(int relatedRequestId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Notifications WHERE related_request_id = @request_id";
                    command.Parameters.AddWithValue("@request_id", relatedRequestId);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
