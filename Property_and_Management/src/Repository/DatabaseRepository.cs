using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.Interface;
using Property_and_Management.src.Model;
using Property_and_Management.src.SQL;

namespace Property_and_Management.src.Repository
{
    public class DatabaseRepository<T> : IRepository<T> where T : notnull, IEntity
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["BoardRent"]?.ConnectionString ?? "";

        public void Add(T newEntity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand())
                {
                    command.CommandText = SqlQueryHelper<T>.CreateInsertQuery(command, newEntity);
                    command.Connection = connection;

                    command.ExecuteNonQuery();
                }
            }
        }

        public T Delete(int removedEntityId)
        {
            T deletedEntity = Get(removedEntityId);

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand())
                {
                    command.CommandText = SqlQueryHelper<T>.CreateDeleteQuery(removedEntityId);
                    command.Connection = connection;

                    command.ExecuteNonQuery();
                }
            }

            return deletedEntity;
        }

        public T Get(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand())
                {
                    command.CommandText = SqlQueryHelper<T>.CreateSelectSpecificIdQuery(id);
                    command.Connection = connection;

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return SqlQueryHelper<T>.EntityFromReader(reader);
                        }

                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        public ImmutableList<T> GetAll()
        {
            List<T> entities = [];

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand())
                {
                    command.CommandText = SqlQueryHelper<T>.CreateSelectAllQuery();
                    command.Connection = connection;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            entities.Add(SqlQueryHelper<T>.EntityFromReader(reader));
                        }
                    }
                }
            }

            return entities.ToImmutableList();
        }

        public ImmutableList<T> SelectWhere(string whereCondition)
        {
            List<T> entities = [];

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand())
                {
                    command.CommandText = SqlQueryHelper<T>.CreateSelectWhereQuery(whereCondition);
                    command.Connection = connection;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            entities.Add(SqlQueryHelper<T>.EntityFromReader(reader));
                        }
                    }
                }
            }

            return entities.ToImmutableList();
        }

        public void Update(int updatedEntityId, T newEntity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand())
                {
                    command.CommandText = SqlQueryHelper<T>.CreateUpdateQuery(command, updatedEntityId, newEntity);
                    command.Connection = connection;

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
