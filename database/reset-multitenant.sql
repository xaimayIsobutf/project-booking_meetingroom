USE RoomBooking;
GO

-- WARNING: ลบโครงสร้างและข้อมูลเดิมของ RoomBooking
IF OBJECT_ID('ReservationCheckLogs','U') IS NOT NULL DROP TABLE ReservationCheckLogs;
IF OBJECT_ID('AuditLogs','U') IS NOT NULL DROP TABLE AuditLogs;
IF OBJECT_ID('ReservationServices','U') IS NOT NULL DROP TABLE ReservationServices;
IF OBJECT_ID('Reservations','U') IS NOT NULL DROP TABLE Reservations;
IF OBJECT_ID('Rooms','U') IS NOT NULL DROP TABLE Rooms;
IF OBJECT_ID('MeetingRooms','U') IS NOT NULL DROP TABLE MeetingRooms;
IF OBJECT_ID('Users','U') IS NOT NULL DROP TABLE Users;
IF OBJECT_ID('Companies','U') IS NOT NULL DROP TABLE Companies;
IF OBJECT_ID('Tenants','U') IS NOT NULL DROP TABLE Tenants;
GO

CREATE TABLE Tenants (TenantId INT IDENTITY PRIMARY KEY, CompanyName NVARCHAR(100) NOT NULL, Subdomain NVARCHAR(50) NOT NULL UNIQUE, LogoUrl NVARCHAR(500), IconUrl NVARCHAR(500), PrimaryColor VARCHAR(7) NOT NULL DEFAULT '#1976D2', SecondaryColor VARCHAR(7) NOT NULL DEFAULT '#424242', BackgroundColor VARCHAR(7) NOT NULL DEFAULT '#FFFFFF', CustomCss NVARCHAR(MAX), SubscriptionPlan NVARCHAR(20) NOT NULL DEFAULT 'Free', Status NVARCHAR(20) NOT NULL DEFAULT 'Active', CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL);
CREATE TABLE Users (UserId INT IDENTITY PRIMARY KEY, TenantId INT NOT NULL, Email NVARCHAR(100) NOT NULL, PasswordHash NVARCHAR(255) NOT NULL, FullName NVARCHAR(100) NOT NULL, Department NVARCHAR(50), PhoneNumber VARCHAR(20), Role NVARCHAR(20) NOT NULL DEFAULT 'Employee', IsActive BIT NOT NULL DEFAULT 1, CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL, CONSTRAINT FK_Users_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE, CONSTRAINT UQ_Users_Tenant_Email UNIQUE (TenantId, Email));
CREATE TABLE Rooms (RoomId INT IDENTITY PRIMARY KEY, TenantId INT NOT NULL, RoomName NVARCHAR(100) NOT NULL, Capacity INT NOT NULL, Location NVARCHAR(150), ImageUrls NVARCHAR(MAX), Amenities NVARCHAR(MAX), BookingNoticeHours INT NOT NULL DEFAULT 1, RequiresApproval BIT NOT NULL DEFAULT 0, Status NVARCHAR(20) NOT NULL DEFAULT 'Active', CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL, CONSTRAINT FK_Rooms_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId));
CREATE TABLE Reservations (ReservationId INT IDENTITY PRIMARY KEY, TenantId INT NOT NULL, RoomId INT NOT NULL, UserId INT NOT NULL, MeetingTitle NVARCHAR(200) NOT NULL, Description NVARCHAR(MAX), StartTime DATETIME2 NOT NULL, EndTime DATETIME2 NOT NULL, AttendeesCount INT NOT NULL DEFAULT 1, AttendeesList NVARCHAR(MAX), Status NVARCHAR(20) NOT NULL DEFAULT 'Approved', IsRecurring BIT NOT NULL DEFAULT 0, RecurrenceRule NVARCHAR(100), RejectionReason NVARCHAR(500), CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL, CONSTRAINT FK_Reservations_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId), CONSTRAINT FK_Reservations_Rooms FOREIGN KEY (RoomId) REFERENCES Rooms(RoomId), CONSTRAINT FK_Reservations_Users FOREIGN KEY (UserId) REFERENCES Users(UserId), CONSTRAINT CHK_Reservation_Time CHECK (EndTime > StartTime));
CREATE TABLE ReservationServices (ServiceRequestId INT IDENTITY PRIMARY KEY, TenantId INT NOT NULL, ReservationId INT NOT NULL, ServiceType NVARCHAR(50) NOT NULL, Quantity INT NOT NULL DEFAULT 1, Note NVARCHAR(250), Status NVARCHAR(20) NOT NULL DEFAULT 'Pending', CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), CONSTRAINT FK_ReservationServices_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId), CONSTRAINT FK_ReservationServices_Reservations FOREIGN KEY (ReservationId) REFERENCES Reservations(ReservationId) ON DELETE CASCADE);
CREATE TABLE AuditLogs (AuditLogId BIGINT IDENTITY PRIMARY KEY, TenantId INT NOT NULL, UserId INT NULL, Action NVARCHAR(50) NOT NULL, EntityName NVARCHAR(50) NOT NULL, EntityId INT NULL, Details NVARCHAR(MAX), IpAddress VARCHAR(45), CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), CONSTRAINT FK_AuditLogs_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId));
CREATE TABLE ReservationCheckLogs (CheckLogId INT IDENTITY PRIMARY KEY, TenantId INT NOT NULL, ReservationId INT NOT NULL, UserId INT NOT NULL, CheckInTime DATETIME2 NULL, CheckOutTime DATETIME2 NULL, IsAutoCancelled BIT NOT NULL DEFAULT 0, CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), CONSTRAINT FK_CheckLogs_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId), CONSTRAINT FK_CheckLogs_Reservations FOREIGN KEY (ReservationId) REFERENCES Reservations(ReservationId) ON DELETE CASCADE, CONSTRAINT FK_CheckLogs_Users FOREIGN KEY (UserId) REFERENCES Users(UserId));
CREATE INDEX IX_Rooms_TenantId ON Rooms(TenantId);
CREATE INDEX IX_Reservations_OverlapCheck ON Reservations(TenantId, RoomId, Status, StartTime, EndTime);
CREATE INDEX IX_Reservations_UserId ON Reservations(TenantId, UserId);
CREATE INDEX IX_AuditLogs_Tenant_Date ON AuditLogs(TenantId, CreatedAt DESC);
GO

-- ข้อมูลเริ่มต้นสำหรับทดสอบ
INSERT INTO Tenants (CompanyName, Subdomain, PrimaryColor, SecondaryColor, BackgroundColor) VALUES (N'Makub Center', 'makub', '#5B2A86', '#24143D', '#F5F2FC');
DECLARE @TenantId INT = SCOPE_IDENTITY();
INSERT INTO Users (TenantId, Email, PasswordHash, FullName, Role) VALUES (@TenantId, 'admin@makub.local', 'CHANGE_THIS_PASSWORD_HASH', N'Saymay SuperAdmin', 'TenantAdmin');
DECLARE @UserId INT = SCOPE_IDENTITY();
INSERT INTO Rooms (TenantId, RoomName, Capacity, Location, Amenities) VALUES (@TenantId, N'Makub A', 8, N'ชั้น 1', N'["TV","Whiteboard"]'), (@TenantId, N'Makub B', 12, N'ชั้น 1', N'["Projector","Video call"]'), (@TenantId, N'Creative Room', 20, N'ชั้น 2', N'["LED","Microphone"]');
GO
