using System;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace Property_and_Management.src.Repository
{
    public static class DatabaseInitializer
    {
        public static void EnsureDatabaseAndTables(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            string databaseName = builder.InitialCatalog;

            // Connect to master to ensure the database exists
            builder.InitialCatalog = "master";
            using (var masterConnection = new SqlConnection(builder.ConnectionString))
            {
                masterConnection.Open();
                EnsureDatabaseExists(masterConnection, databaseName);
            }

            // Connect to the target database and ensure all tables exist, then seed if empty
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                EnsureTablesExist(connection);
                SeedDataIfEmpty(connection);
            }

            Debug.WriteLine($"DatabaseInitializer: '{databaseName}' is ready.");
        }

        private static void EnsureDatabaseExists(SqlConnection masterConnection, string databaseName)
        {
            using var cmd = masterConnection.CreateCommand();
            cmd.CommandText = $@"
                IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = @name)
                BEGIN
                    CREATE DATABASE [{databaseName}];
                END";
            cmd.Parameters.AddWithValue("@name", databaseName);
            cmd.ExecuteNonQuery();
        }

        private static void EnsureTablesExist(SqlConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                IF OBJECT_ID(N'[dbo].[Users]', 'U') IS NULL
                BEGIN
                    CREATE TABLE Users (
                        id INT IDENTITY(1,1) NOT NULL,
                        display_name VARCHAR(50) NOT NULL DEFAULT 'Unknown User',
                        CONSTRAINT PK_User PRIMARY KEY (id)
                    );
                END;

                IF OBJECT_ID(N'[dbo].[Games]', 'U') IS NULL
                BEGIN
                    CREATE TABLE Games (
                        game_id INT IDENTITY(1,1) NOT NULL,
                        owner_id INT NOT NULL,
                        name VARCHAR(30) NOT NULL,
                        price DECIMAL(5,2) NOT NULL,
                        minimum_player_number INT NOT NULL,
                        maximum_player_number INT NOT NULL,
                        description VARCHAR(500) NOT NULL,
                        image VARBINARY(MAX),
                        is_active INT NOT NULL DEFAULT 1,

                        CONSTRAINT PK_Games PRIMARY KEY (game_id),
                        CONSTRAINT FK_Games_Owner FOREIGN KEY (owner_id) REFERENCES [Users](id),

                        CONSTRAINT CHK_Game_Price CHECK (price > 0),
                        CONSTRAINT CHK_Min_Players CHECK (minimum_player_number >= 1),
                        CONSTRAINT CHK_Max_Players CHECK (maximum_player_number >= 1 AND maximum_player_number >= minimum_player_number)
                    );
                END;

                IF OBJECT_ID(N'[dbo].[Requests]', 'U') IS NULL
                BEGIN
                    CREATE TABLE Requests (
                        request_id INT IDENTITY(1,1) NOT NULL,
                        game_id INT NOT NULL,
                        renter_id INT NOT NULL,
                        owner_id INT NOT NULL,
                        start_date DATETIME NOT NULL,
                        end_date DATETIME NOT NULL,

                        CONSTRAINT PK_Request PRIMARY KEY (request_id),
                        CONSTRAINT FK_Request_Game FOREIGN KEY (game_id) REFERENCES Games(game_id),
                        CONSTRAINT FK_Request_Renter FOREIGN KEY (renter_id) REFERENCES [Users](id),
                        CONSTRAINT FK_Request_Owner FOREIGN KEY (owner_id) REFERENCES [Users](id),
                        CONSTRAINT CHK_Request_DateRange CHECK (end_date >= start_date)
                    );
                END;

                IF OBJECT_ID(N'[dbo].[Rentals]', 'U') IS NULL
                BEGIN
                    CREATE TABLE Rentals (
                        rental_id INT IDENTITY(1,1) NOT NULL,
                        game_id INT NOT NULL,
                        renter_id INT NOT NULL,
                        owner_id INT NOT NULL,
                        start_date DATETIME NOT NULL,
                        end_date DATETIME NOT NULL,

                        CONSTRAINT PK_Rentals PRIMARY KEY (rental_id),
                        CONSTRAINT FK_Rentals_Game FOREIGN KEY (game_id) REFERENCES Games(game_id),
                        CONSTRAINT FK_Rentals_Renter FOREIGN KEY (renter_id) REFERENCES [Users](id),
                        CONSTRAINT FK_Rentals_Owner FOREIGN KEY (owner_id) REFERENCES [Users](id),
                        CONSTRAINT CHK_Rentals_DateRange CHECK (end_date >= start_date)
                    );
                END;

                IF OBJECT_ID(N'[dbo].[Notifications]', 'U') IS NULL
                BEGIN
                    CREATE TABLE Notifications (
                        notification_id INT IDENTITY(1,1) NOT NULL,
                        user_id INT NOT NULL,
                        timestamp DATETIME NOT NULL,
                        title VARCHAR(30) NOT NULL,
                        body VARCHAR(500) NOT NULL,

                        CONSTRAINT PK_Notifications PRIMARY KEY (notification_id),
                        CONSTRAINT FK_Notifications_User FOREIGN KEY (user_id) REFERENCES [Users](id)
                    );
                END;

                -- Ensure display_name column exists (handles older schema versions)
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'display_name'
                )
                BEGIN
                    ALTER TABLE Users ADD display_name VARCHAR(50) NOT NULL DEFAULT 'Unknown User';
                END;
            ";
            cmd.ExecuteNonQuery();
        }

        private static void SeedDataIfEmpty(SqlConnection connection)
        {
            // Only seed if the Users table is empty (first run)
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Users";
            int userCount = (int)checkCmd.ExecuteScalar();
            if (userCount > 0)
                return;

            Debug.WriteLine("DatabaseInitializer: Seeding initial data...");

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                -- Users
                SET IDENTITY_INSERT Users ON;
                INSERT INTO Users (id, display_name) VALUES (1, 'Darius Turcu'), (2, 'Mihai Tira');
                SET IDENTITY_INSERT Users OFF;

                -- Games (11 entries, mixed owners)
                SET IDENTITY_INSERT Games ON;
                INSERT INTO Games (game_id, owner_id, name, price, minimum_player_number, maximum_player_number, description, is_active) VALUES
                (1,  1, 'Catan Base Game',         15.50, 3, 4, 'Classic resource management and trading board game.', 1),
                (2,  1, 'Ticket to Ride Europe',   12.00, 2, 5, 'Build train routes across Europe in this classic game.', 1),
                (3,  1, 'Carcassonne',             10.00, 2, 5, 'Tile-placement game where you build medieval cities.', 1),
                (4,  1, 'Pandemic',                18.00, 2, 4, 'Cooperative game where you save the world from diseases.', 1),
                (5,  1, 'Splendor',                14.00, 2, 4, 'Engine-building game involving gem collecting and cards.', 1),
                (6,  2, '7 Wonders',               20.00, 2, 7, 'Draft cards to build your ancient civilization.', 1),
                (7,  2, 'Dominion',                16.00, 2, 4, 'The original deck-building card game.', 1),
                (8,  2, 'Codenames',                8.50, 2, 8, 'Word association party game for two teams.', 1),
                (9,  2, 'Wingspan',                22.00, 1, 5, 'Engine-building game based on bird watching.', 1),
                (10, 2, 'Terraforming Mars',       25.00, 1, 5, 'Compete to make Mars habitable for humanity.', 1),
                (11, 1, 'Twilight Imperium',       40.00, 3, 6, 'Epic space opera board game of galactic conquest.', 0);
                SET IDENTITY_INSERT Games OFF;

                -- Requests (11 entries with relative dates)
                SET IDENTITY_INSERT Requests ON;
                INSERT INTO Requests (request_id, game_id, renter_id, owner_id, start_date, end_date) VALUES
                (1,  1,  2, 1, DATEADD(day,  5, GETDATE()), DATEADD(day,  7, GETDATE())),
                (2,  1,  2, 1, DATEADD(day,  6, GETDATE()), DATEADD(day,  8, GETDATE())),
                (3,  2,  2, 1, DATEADD(day, 10, GETDATE()), DATEADD(day, 12, GETDATE())),
                (4,  2,  2, 1, DATEADD(day, 20, GETDATE()), DATEADD(day, 22, GETDATE())),
                (5,  3,  2, 1, DATEADD(day,  1, GETDATE()), DATEADD(day,  3, GETDATE())),
                (6,  6,  1, 2, DATEADD(day,  5, GETDATE()), DATEADD(day,  7, GETDATE())),
                (7,  7,  1, 2, DATEADD(day, 15, GETDATE()), DATEADD(day, 18, GETDATE())),
                (8,  8,  1, 2, DATEADD(day,  2, GETDATE()), DATEADD(day,  4, GETDATE())),
                (9,  9,  1, 2, DATEADD(day,  8, GETDATE()), DATEADD(day, 10, GETDATE())),
                (10, 10, 1, 2, DATEADD(day, 25, GETDATE()), DATEADD(day, 28, GETDATE())),
                (11, 10, 1, 2, DATEADD(day, 26, GETDATE()), DATEADD(day, 29, GETDATE()));
                SET IDENTITY_INSERT Requests OFF;

                -- Rentals (10 entries, mix of past/active/future)
                SET IDENTITY_INSERT Rentals ON;
                INSERT INTO Rentals (rental_id, game_id, renter_id, owner_id, start_date, end_date) VALUES
                (1,  4,  2, 1, DATEADD(day, -10, GETDATE()), DATEADD(day,  -8, GETDATE())),
                (2,  5,  2, 1, DATEADD(day,  -5, GETDATE()), DATEADD(day,  -3, GETDATE())),
                (3,  1,  2, 1, DATEADD(day, -20, GETDATE()), DATEADD(day, -15, GETDATE())),
                (4,  2,  2, 1, DATEADD(day,   1, GETDATE()), DATEADD(day,   4, GETDATE())),
                (5,  3,  2, 1, DATEADD(day,  15, GETDATE()), DATEADD(day,  17, GETDATE())),
                (6,  6,  1, 2, DATEADD(day, -12, GETDATE()), DATEADD(day, -10, GETDATE())),
                (7,  7,  1, 2, DATEADD(day,  -8, GETDATE()), DATEADD(day,  -6, GETDATE())),
                (8,  8,  1, 2, DATEADD(day,  -2, GETDATE()), DATEADD(day,   2, GETDATE())),
                (9,  9,  1, 2, DATEADD(day,  10, GETDATE()), DATEADD(day,  12, GETDATE())),
                (10, 10, 1, 2, DATEADD(day,  20, GETDATE()), DATEADD(day,  23, GETDATE()));
                SET IDENTITY_INSERT Rentals OFF;

                -- Notifications (10 entries)
                SET IDENTITY_INSERT Notifications ON;
                INSERT INTO Notifications (notification_id, user_id, timestamp, title, body) VALUES
                (1,  1, DATEADD(day, -5, GETDATE()), 'New Message',               'User 2'),
                (2,  1, DATEADD(day, -4, GETDATE()), 'Upcoming Rental Reminder',  'Catan Base Game rental starts soon.'),
                (3,  1, DATEADD(day, -3, GETDATE()), 'Rental Request Declined',   'Twilight Imperium request declined.'),
                (4,  1, DATEADD(day, -2, GETDATE()), 'New Message',               'User 2'),
                (5,  1, DATEADD(day, -1, GETDATE()), 'Booking Unavailable',       'Someone else booked 7 Wonders.'),
                (6,  2, DATEADD(day, -5, GETDATE()), 'New Message',               'User 1'),
                (7,  2, DATEADD(day, -4, GETDATE()), 'Upcoming Rental Reminder',  '7 Wonders rental starts soon.'),
                (8,  2, DATEADD(day, -3, GETDATE()), 'Rental Request Declined',   'Dominion request declined.'),
                (9,  2, DATEADD(day, -2, GETDATE()), 'New Message',               'User 1'),
                (10, 2, DATEADD(day, -1, GETDATE()), 'Booking Unavailable',       'Someone else booked Catan Base Game.');
                SET IDENTITY_INSERT Notifications OFF;

                -- Sync identity columns so new inserts start from the right IDs
                DBCC CHECKIDENT ('Users',        RESEED, 2);
                DBCC CHECKIDENT ('Games',        RESEED, 11);
                DBCC CHECKIDENT ('Requests',     RESEED, 11);
                DBCC CHECKIDENT ('Rentals',      RESEED, 10);
                DBCC CHECKIDENT ('Notifications', RESEED, 10);
            ";
            cmd.ExecuteNonQuery();
        }
    }
}
