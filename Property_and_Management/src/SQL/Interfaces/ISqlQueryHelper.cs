using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Property_and_Management.src.Interface;

namespace Property_and_Management.src.SQL.Interfaces
{
    public interface ISqlQueryHelper<T>
    {
        /// <summary>
        /// Generates a parameterized SQL INSERT statement and attaches the variable values to the command.
        /// </summary>
        /// <param name="command">The command to attach parameters to.</param>
        /// <param name="entity">The entity containing the data to insert.</param>
        /// <param name="includePrimaryKey">Whether to explicitly insert the primary key.</param>
        /// <returns>The SQL INSERT string.</returns>
        static abstract string CreateInsertQuery(SqlCommand command, T entity, bool includePrimaryKey = false);

        /// <summary>
        /// Generates a parameterized SQL UPDATE statement and attaches the variable values to the command.
        /// </summary>
        /// <param name="command">The command to attach parameters to.</param>
        /// <param name="entityId">The ID of the record to update.</param>
        /// <param name="newEntity">The entity containing the updated data.</param>
        /// <param name="includePrimaryKey">Whether to include the primary key in the update fields.</param>
        /// <returns>The SQL UPDATE string.</returns>
        static abstract string CreateUpdateQuery(SqlCommand command, int entityId, T newEntity, bool includePrimaryKey = false);

        /// <summary>
        /// Generates a SQL query to select all records from the entity's table.
        /// </summary>
        /// <returns>The SQL SELECT string.</returns>
        static abstract string CreateSelectAllQuery();

        /// <summary>
        /// Generates a SQL query to select a specific record by its primary key.
        /// </summary>
        /// <param name="entityId">The target primary key ID.</param>
        /// <returns>The SQL SELECT string.</returns>
        static abstract string CreateSelectSpecificIdQuery(int entityId);

        /// <summary>
        /// Generates a SQL SELECT query using a custom WHERE clause.
        /// </summary>
        /// <param name="whereParameters">The raw SQL WHERE condition (e.g., "Age > 30").</param>
        /// <returns>The SQL SELECT string.</returns>
        static abstract string CreateSelectWhereQuery(string whereParameters);

        /// <summary>
        /// Generates a SQL query to delete a specific record by its primary key.
        /// </summary>
        /// <param name="entityId">The target primary key ID.</param>
        /// <returns>The SQL DELETE string.</returns>
        static abstract string CreateDeleteQuery(int entityId);

        /// <summary>
        /// Gets the mapped SQL column name for the entity's primary key.
        /// </summary>
        /// <returns>The primary key column name.</returns>
        static abstract string GetPrimaryKeyField();

        /// <summary>
        /// Gets a list of mapped SQL column names for the entity.
        /// </summary>
        /// <param name="includePrimaryKey">Whether to include the primary key column in the list.</param>
        /// <returns>A list of SQL column names.</returns>
        static abstract List<string> GetTableFields(bool includePrimaryKey);

        /// <summary>
        /// Gets the mapped SQL table name for the entity.
        /// </summary>
        /// <returns>The table name.</returns>
        static abstract string GetTableName();

        /// <summary>
        /// Extracts a dictionary of SQL parameter names and their corresponding entity values.
        /// </summary>
        /// <param name="entity">The entity to extract values from.</param>
        /// <param name="includePrimaryKey">Whether to include the primary key in the dictionary.</param>
        /// <returns>A dictionary of parameter names (keys) and values.</returns>
        static abstract Dictionary<string, object?> GetVariables(T entity, bool includePrimaryKey);

        /// <summary>
        /// Creates a new entity instance and populates it using the current row of the reader.
        /// </summary>
        /// <param name="reader">The active database reader.</param>
        /// <returns>The populated entity.</returns>
        static abstract T EntityFromReader(SqlDataReader reader);
    }
}
