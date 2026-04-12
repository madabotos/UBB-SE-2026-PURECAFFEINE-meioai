using System;
using System.Configuration;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Property_and_Management.Src.Repository;

namespace Property_and_Management.Tests.Repository
{
    // Base class for repository integration tests. Reads the BoardRent
    // connection string from the test project's App.config (which points at
    // BoardRent_Test — a dedicated database that can be freely wiped).
    // Tests derived from this class must be marked [Category("Integration")]
    // so they can be filtered out of normal unit test runs.
    public abstract class DatabaseTestBase
    {
        private const string ConnectionStringName = "BoardRent";

        protected string ConnectionString { get; private set; } = null!;

        [OneTimeSetUp]
        public void InitializeDatabase()
        {
            ConnectionString = ConfigurationManager
                .ConnectionStrings[ConnectionStringName]?.ConnectionString
                ?? throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' is missing from App.config.");

            try
            {
                DatabaseInitializer.EnsureDatabaseInitialized();
            }
            catch (SqlException sqlException)
            {
                Assert.Ignore(
                    "Skipping integration tests: SQL Server is not reachable. "
                    + $"Error: {sqlException.Message}");
            }
        }

        [SetUp]
        public void TruncateBusinessTables()
        {
            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "DELETE FROM Notifications;"
                + "DELETE FROM Rentals;"
                + "DELETE FROM Requests;"
                + "DBCC CHECKIDENT ('Notifications', RESEED, 0);"
                + "DBCC CHECKIDENT ('Rentals', RESEED, 0);"
                + "DBCC CHECKIDENT ('Requests', RESEED, 0);";
            command.ExecuteNonQuery();
        }
    }
}
