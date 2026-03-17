using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.Attributes;
using Property_and_Management.src.Interface;
using Property_and_Management.src.SQL.Interfaces;
using VariableDictionary = System.Collections.Generic.Dictionary<string, object?>;

namespace Property_and_Management.src.SQL
{
    public class SqlQueryHelper<T> : ISqlQueryHelper<T> where T : IEntity
    {
        private struct SqlQueryHelperParameters(string primaryKeyField, string tableName, List<string> fieldNames, VariableDictionary variables)
        {
            public string PrimaryKeyField { get; } = primaryKeyField;
            public string TableName { get; } = tableName;
            public List<string> FieldNames { get; } = fieldNames;
            public VariableDictionary Variables { get; } = variables;
        }

        private static SqlQueryHelperParameters GetHelperParameters(T entity, bool includePrimaryKey)
        {
            var primaryKeyField = GetPrimaryKeyField();
            var tableName = GetTableName();
            var fieldNames = GetTableFields(includePrimaryKey);
            var variables = GetVariables(entity, includePrimaryKey);

            return new SqlQueryHelperParameters(primaryKeyField, tableName, fieldNames, variables);
        }

        private static void UpdateCommandParameters(SqlCommand command, VariableDictionary variableDictionary)
        {
            foreach (var keyValuePair in variableDictionary)
            {
                var value = keyValuePair.Value ?? DBNull.Value;
                command.Parameters.AddWithValue(keyValuePair.Key, value);
            }
        }

        public static string GetTableName()
        {
            var genericType = typeof(T);

            // Get the table name
            var tableNameAttribute = Attribute.GetCustomAttribute(genericType, typeof(SqlTableDefinitionAttribute)) as SqlTableDefinitionAttribute;

            if (tableNameAttribute == null)
            {
                throw new KeyNotFoundException($"Class has no {nameof(SqlTableDefinitionAttribute)} attribute!");
            }

            return tableNameAttribute.TableName;
        }

        public static List<string> GetTableFields(bool includePrimaryKey)
        {
            var genericType = typeof(T);

            List<string> fieldNames = [];

            // Go through each property
            foreach (var property in genericType.GetProperties())
            {
                var fieldNameAttribute = property.GetCustomAttribute<SqlTableFieldDefinitionAttribute>();

                if (fieldNameAttribute == null)
                {
                    throw new KeyNotFoundException($"Property has no {nameof(SqlTableFieldDefinitionAttribute)} attribute!");
                }

                if (fieldNameAttribute.IsPrimaryKey)
                {
                    if (includePrimaryKey)
                    {
                        fieldNames.Add(fieldNameAttribute.FieldName);
                    }
                }
                else
                {
                    fieldNames.Add(fieldNameAttribute.FieldName);
                }
            }

            return fieldNames;
        }

        public static VariableDictionary GetVariables(T entity, bool includePrimaryKey)
        {
            var genericType = typeof(T);

            VariableDictionary variables = [];

            // Go through each property
            foreach (var property in genericType.GetProperties())
            {
                var fieldNameAttribute = property.GetCustomAttribute<SqlTableFieldDefinitionAttribute>();
                object? propertyValue = entity != null ? property.GetValue(entity) : null;

                if (fieldNameAttribute == null)
                {
                    throw new KeyNotFoundException($"Property has no {nameof(SqlTableFieldDefinitionAttribute)} attribute!");
                }

                if (fieldNameAttribute.IsPrimaryKey)
                {
                    if (includePrimaryKey)
                    {
                        variables.Add(fieldNameAttribute.VariableName, propertyValue);
                    }
                }
                else
                {
                    variables.Add(fieldNameAttribute.VariableName, propertyValue);
                }
            }

            return variables;
        }

        public static string GetPrimaryKeyField()
        {
            var genericType = typeof(T);


            // Go through each property
            foreach (var property in genericType.GetProperties())
            {
                var fieldNameAttribute = property.GetCustomAttribute<SqlTableFieldDefinitionAttribute>();

                if (fieldNameAttribute == null)
                {
                    throw new KeyNotFoundException($"Property has no {nameof(SqlTableFieldDefinitionAttribute)} attribute!");
                }

                if (fieldNameAttribute.IsPrimaryKey)
                {
                    return fieldNameAttribute.FieldName;
                }
            }

            throw new KeyNotFoundException($"Entity has no primary key!");
        }


        public static string CreateInsertQuery(SqlCommand command, T entity, bool includePrimaryKey = false)
        {
            var helperParameters = GetHelperParameters(entity, includePrimaryKey);

            string concatenatedFieldNames = string.Join(", ", helperParameters.FieldNames);
            string concatenatedVariableNames = string.Join(", ", helperParameters.Variables.Keys);

            // Create the insert statement
            string insertStatement = $"INSERT INTO {helperParameters.TableName}({concatenatedFieldNames}) VALUES ({concatenatedVariableNames})";

            UpdateCommandParameters(command, helperParameters.Variables);

            return insertStatement;
        }

        public static string CreateUpdateQuery(SqlCommand command, int entityId, T newEntity, bool includePrimaryKey = false)
        {
            var helperParameters = GetHelperParameters(newEntity, includePrimaryKey);

            // Build the columns to the values
            List<string> updateColumnsStringList = [];
            List<string> variableNames = helperParameters.Variables.Keys.ToList();

            // Make sure that we have the same number of parameters
            Debug.Assert(helperParameters.FieldNames.Count == helperParameters.Variables.Count);

            for (int fieldIndex = 0; fieldIndex < helperParameters.FieldNames.Count; fieldIndex++)
            {
                updateColumnsStringList.Add($"{helperParameters.FieldNames[fieldIndex]} = {variableNames[fieldIndex]}");
            }

            string updateColumnString = string.Join(", ", updateColumnsStringList);

            // Create the update statement
            string updateStatement = $"UPDATE {helperParameters.TableName} SET {updateColumnString} WHERE {helperParameters.PrimaryKeyField} = {entityId}";

            UpdateCommandParameters(command, helperParameters.Variables);

            return updateStatement;
        }

        public static string CreateSelectAllQuery()
        {
            var tableName = GetTableName();

            return $"SELECT * FROM {tableName}";
        }

        public static string CreateDeleteQuery(int entityId)
        {
            var tableName = GetTableName();
            var primaryKeyField = GetPrimaryKeyField();

            return $"DELETE FROM {tableName} WHERE {primaryKeyField} = {entityId}";
        }

        public static string CreateSelectSpecificIdQuery(int entityId)
        {
            var tableName = GetTableName();
            var primaryKeyField = GetPrimaryKeyField();

            return $"SELECT * FROM {tableName} WHERE {primaryKeyField} = {entityId}";
        }

        public static string CreateSelectWhereQuery(string whereParameters)
        {
            var tableName = GetTableName();
            return $"SELECT * FROM {tableName} WHERE {whereParameters}";
        }

        public static T EntityFromReader(SqlDataReader reader)
        {

            var genericType = typeof(T);

            Dictionary<string, object> parameters = [];

            foreach (var property in genericType.GetProperties())
            {
                var fieldNameAttribute = property.GetCustomAttribute<SqlTableFieldDefinitionAttribute>();

                if (fieldNameAttribute == null)
                {
                    throw new KeyNotFoundException($"Property has no {nameof(SqlTableFieldDefinitionAttribute)} attribute!");
                }

                try
                {
                    if (reader[fieldNameAttribute.FieldName] is DBNull)
                    {
                        continue;
                    }

                    parameters[fieldNameAttribute.FieldName] = Convert.ChangeType(
                        reader[fieldNameAttribute.FieldName],
                        property.PropertyType
                    );
                }
                catch (IndexOutOfRangeException exception)
                {
                    Console.Write(exception.Message);
                }

            }

            return (T)T.BuildFromParameters(parameters);
        }
    }
}
