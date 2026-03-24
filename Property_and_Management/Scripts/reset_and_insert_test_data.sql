USE BoardRent;
GO

-- 1. Reset all data (delete in reverse dependency order)
DELETE FROM Notifications;
DELETE FROM Rentals;
DELETE FROM Requests;
DELETE FROM Games;
DELETE FROM Users;

-- 2. Insert Test Data for Users 1 and 2
SET IDENTITY_INSERT Users ON;
INSERT INTO Users (id) VALUES (1), (2);
SET IDENTITY_INSERT Users OFF;
GO

-- 3. Insert Games (at least 10 entries, mixed owners)
-- Requirements: 5-30 char name, >0 price, min >= 1, max >= min, 10-500 char description
SET IDENTITY_INSERT Games ON;
INSERT INTO Games (game_id, owner_id, name, price, minimum_player_number, maximum_player_number, description, is_active) VALUES
(1, 1, 'Catan Base Game', 15.50, 3, 4, 'Classic resource management and trading board game.', 1),
(2, 1, 'Ticket to Ride Europe', 12.00, 2, 5, 'Build train routes across Europe in this classic game.', 1),
(3, 1, 'Carcassonne', 10.00, 2, 5, 'Tile-placement game where you build medieval cities.', 1),
(4, 1, 'Pandemic', 18.00, 2, 4, 'Cooperative game where you save the world from diseases.', 1),
(5, 1, 'Splendor', 14.00, 2, 4, 'Engine-building game involving gem collecting and cards.', 1),
(6, 2, '7 Wonders', 20.00, 2, 7, 'Draft cards to build your ancient civilization.', 1),
(7, 2, 'Dominion', 16.00, 2, 4, 'The original deck-building card game.', 1),
(8, 2, 'Codenames', 8.50, 2, 8, 'Word association party game for two teams.', 1),
(9, 2, 'Wingspan', 22.00, 1, 5, 'Engine-building game based on bird watching.', 1),
(10, 2, 'Terraforming Mars', 25.00, 1, 5, 'Compete to make Mars habitable for humanity.', 1),
(11, 1, 'Twilight Imperium', 40.00, 3, 6, 'Epic space opera board game of galactic conquest.', 0); -- Inactive to test REQ-GAM-05
SET IDENTITY_INSERT Games OFF;
GO

-- 4. Insert Requests (at least 10 entries)
-- Test different dates, overlapping tests for SYS-INT-04, etc.
SET IDENTITY_INSERT Requests ON;
INSERT INTO Requests (request_id, game_id, renter_id, owner_id, start_date, end_date) VALUES
(1, 1, 2, 1, DATEADD(day, 5, GETDATE()), DATEADD(day, 7, GETDATE())),
(2, 1, 2, 1, DATEADD(day, 6, GETDATE()), DATEADD(day, 8, GETDATE())), -- Overlaps with 1
(3, 2, 2, 1, DATEADD(day, 10, GETDATE()), DATEADD(day, 12, GETDATE())),
(4, 2, 2, 1, DATEADD(day, 20, GETDATE()), DATEADD(day, 22, GETDATE())),
(5, 3, 2, 1, DATEADD(day, 1, GETDATE()), DATEADD(day, 3, GETDATE())),
(6, 6, 1, 2, DATEADD(day, 5, GETDATE()), DATEADD(day, 7, GETDATE())),
(7, 7, 1, 2, DATEADD(day, 15, GETDATE()), DATEADD(day, 18, GETDATE())),
(8, 8, 1, 2, DATEADD(day, 2, GETDATE()), DATEADD(day, 4, GETDATE())),
(9, 9, 1, 2, DATEADD(day, 8, GETDATE()), DATEADD(day, 10, GETDATE())),
(10, 10, 1, 2, DATEADD(day, 25, GETDATE()), DATEADD(day, 28, GETDATE())),
(11, 10, 1, 2, DATEADD(day, 26, GETDATE()), DATEADD(day, 29, GETDATE())); -- Overlaps with 10
SET IDENTITY_INSERT Requests OFF;
GO

-- 5. Insert Rentals (at least 10 entries)
-- Mix of past (greyed out - REQ-REN-04) and future/active rentals
SET IDENTITY_INSERT Rentals ON;
INSERT INTO Rentals (rental_id, game_id, renter_id, owner_id, start_date, end_date) VALUES
(1, 4, 2, 1, DATEADD(day, -10, GETDATE()), DATEADD(day, -8, GETDATE())), -- Past rental
(2, 5, 2, 1, DATEADD(day, -5, GETDATE()), DATEADD(day, -3, GETDATE())),  -- Past rental
(3, 1, 2, 1, DATEADD(day, -20, GETDATE()), DATEADD(day, -15, GETDATE())), -- Past rental
(4, 2, 2, 1, DATEADD(day, 1, GETDATE()), DATEADD(day, 4, GETDATE())),    -- Active/Future rental
(5, 3, 2, 1, DATEADD(day, 15, GETDATE()), DATEADD(day, 17, GETDATE())),  -- Future rental
(6, 6, 1, 2, DATEADD(day, -12, GETDATE()), DATEADD(day, -10, GETDATE())), -- Past rental
(7, 7, 1, 2, DATEADD(day, -8, GETDATE()), DATEADD(day, -6, GETDATE())),   -- Past rental
(8, 8, 1, 2, DATEADD(day, -2, GETDATE()), DATEADD(day, 2, GETDATE())),    -- Currently active rental
(9, 9, 1, 2, DATEADD(day, 10, GETDATE()), DATEADD(day, 12, GETDATE())),   -- Future rental
(10, 10, 1, 2, DATEADD(day, 20, GETDATE()), DATEADD(day, 23, GETDATE())); -- Future rental
SET IDENTITY_INSERT Rentals OFF;
GO

-- 6. Insert Notifications (at least 10 entries)
SET IDENTITY_INSERT Notifications ON;
INSERT INTO Notifications (notification_id, user_id, timestamp, title, body) VALUES
(1, 1, DATEADD(day, -5, GETDATE()), 'New Message', 'User 2'),
(2, 1, DATEADD(day, -4, GETDATE()), 'Upcoming Rental Reminder', 'Catan Base Game rental starts soon.'),
(3, 1, DATEADD(day, -3, GETDATE()), 'Rental Request Declined', 'Twilight Imperium request declined.'),
(4, 1, DATEADD(day, -2, GETDATE()), 'New Message', 'User 2'),
(5, 1, DATEADD(day, -1, GETDATE()), 'Booking Unavailable', 'Someone else booked 7 Wonders.'),
(6, 2, DATEADD(day, -5, GETDATE()), 'New Message', 'User 1'),
(7, 2, DATEADD(day, -4, GETDATE()), 'Upcoming Rental Reminder', '7 Wonders rental starts soon.'),
(8, 2, DATEADD(day, -3, GETDATE()), 'Rental Request Declined', 'Dominion request declined.'),
(9, 2, DATEADD(day, -2, GETDATE()), 'New Message', 'User 1'),
(10, 2, DATEADD(day, -1, GETDATE()), 'Booking Unavailable', 'Someone else booked Catan Base Game.');
SET IDENTITY_INSERT Notifications OFF;
GO

-- 7. Sync Identity Columns
-- This ensures that when your C# app inserts new records, it starts from the correct ID.
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DBCC CHECKIDENT ('Users', RESEED, 2);
IF OBJECT_ID('dbo.Games', 'U') IS NOT NULL DBCC CHECKIDENT ('Games', RESEED, 11);
IF OBJECT_ID('dbo.Requests', 'U') IS NOT NULL DBCC CHECKIDENT ('Requests', RESEED, 11);
IF OBJECT_ID('dbo.Rentals', 'U') IS NOT NULL DBCC CHECKIDENT ('Rentals', RESEED, 10);
IF OBJECT_ID('dbo.Notifications', 'U') IS NOT NULL DBCC CHECKIDENT ('Notifications', RESEED, 10);
GO
