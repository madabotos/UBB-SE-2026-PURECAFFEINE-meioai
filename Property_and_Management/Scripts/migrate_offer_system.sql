/*
Migration script for the Offer System feature.
Adds status and offering_user_id to Requests table.
Adds type and related_request_id to Notifications table.

Run in SSMS against an existing BoardRent database.
Safe to run multiple times (idempotent).
*/

USE BoardRent;
GO

-- 1. Add status column to Requests (0=Open, 1=OfferPending, 2=Accepted, 3=Cancelled)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Requests') AND name = 'status')
BEGIN
    ALTER TABLE Requests ADD status INT NOT NULL DEFAULT 0;
    PRINT 'Added status column to Requests';
END;
GO

-- 2. Add offering_user_id to Requests (nullable FK to Users)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Requests') AND name = 'offering_user_id')
BEGIN
    ALTER TABLE Requests ADD offering_user_id INT NULL;
    ALTER TABLE Requests ADD CONSTRAINT FK_Request_OfferingUser
        FOREIGN KEY (offering_user_id) REFERENCES [Users](id);
    PRINT 'Added offering_user_id column to Requests';
END;
GO

-- 3. Add type column to Notifications (0=Informational, 1=OfferReceived, 2=OfferResult)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Notifications') AND name = 'type')
BEGIN
    ALTER TABLE Notifications ADD type INT NOT NULL DEFAULT 0;
    PRINT 'Added type column to Notifications';
END;
GO

-- 4. Add related_request_id to Notifications (nullable FK to Requests)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Notifications') AND name = 'related_request_id')
BEGIN
    ALTER TABLE Notifications ADD related_request_id INT NULL;
    ALTER TABLE Notifications ADD CONSTRAINT FK_Notification_Request
        FOREIGN KEY (related_request_id) REFERENCES Requests(request_id);
    PRINT 'Added related_request_id column to Notifications';
END;
GO

PRINT 'Offer system migration complete.';
GO
