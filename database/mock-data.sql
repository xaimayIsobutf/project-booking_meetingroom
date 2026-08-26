USE RoomBooking;
GO

-- Companies / Tenants
IF NOT EXISTS (SELECT 1 FROM Tenants WHERE Subdomain = 'acme') INSERT INTO Tenants (CompanyName, Subdomain, PrimaryColor, SecondaryColor, BackgroundColor) VALUES (N'Acme Thailand', 'acme', '#315BE8', '#172033', '#F4F7FB');
IF NOT EXISTS (SELECT 1 FROM Tenants WHERE Subdomain = 'lbb') INSERT INTO Tenants (CompanyName, Subdomain, PrimaryColor, SecondaryColor, BackgroundColor) VALUES (N'LBB Finance', 'lbb', '#
', '#24143D', '#F5F2FC');

DECLARE @Makub INT = (SELECT TenantId FROM Tenants WHERE Subdomain = 'makub');
DECLARE @Acme INT = (SELECT TenantId FROM Tenants WHERE Subdomain = 'acme');
DECLARE @Lbb INT = (SELECT TenantId FROM Tenants WHERE Subdomain = 'lbb');

-- Users: PasswordHash เป็นค่า Mockup ชั่วคราว ระบบ Login จริงจะใช้ Hash ที่ปลอดภัย
IF NOT EXISTS (SELECT 1 FROM Users WHERE TenantId=@Acme AND Email='admin@acme.local') INSERT INTO Users (TenantId,Email,PasswordHash,FullName,Department,Role) VALUES (@Acme,'admin@acme.local','MOCK_HASH',N'Anna Admin',N'Administration','TenantAdmin');
IF NOT EXISTS (SELECT 1 FROM Users WHERE TenantId=@Acme AND Email='employee@acme.local') INSERT INTO Users (TenantId,Email,PasswordHash,FullName,Department,Role) VALUES (@Acme,'employee@acme.local','MOCK_HASH',N'John Employee',N'Product','Employee');
IF NOT EXISTS (SELECT 1 FROM Users WHERE TenantId=@Lbb AND Email='approver@lbb.local') INSERT INTO Users (TenantId,Email,PasswordHash,FullName,Department,Role) VALUES (@Lbb,'approver@lbb.local','MOCK_HASH',N'ณัฐวุฒิ ผู้อนุมัติ',N'Compliance','Approver');

-- Rooms
IF NOT EXISTS (SELECT 1 FROM Rooms WHERE TenantId=@Acme AND RoomName=N'Blue Ocean') INSERT INTO Rooms (TenantId,RoomName,Capacity,Location,Amenities,RequiresApproval) VALUES (@Acme,N'Blue Ocean',6,N'ชั้น 3 อาคาร A',N'["TV","Whiteboard"]',0),(@Acme,N'Boardroom',16,N'ชั้น 4 อาคาร A',N'["Projector","Video call","Microphone"]',1);
IF NOT EXISTS (SELECT 1 FROM Rooms WHERE TenantId=@Lbb AND RoomName=N'Compliance Room') INSERT INTO Rooms (TenantId,RoomName,Capacity,Location,Amenities,RequiresApproval) VALUES (@Lbb,N'Compliance Room',10,N'ชั้น 2 อาคาร B',N'["TV","Whiteboard"]',1);

-- Sample reservations
DECLARE @AcmeUser INT = (SELECT TOP 1 UserId FROM Users WHERE TenantId=@Acme AND Email='employee@acme.local');
DECLARE @BlueRoom INT = (SELECT TOP 1 RoomId FROM Rooms WHERE TenantId=@Acme AND RoomName=N'Blue Ocean');
IF NOT EXISTS (SELECT 1 FROM Reservations WHERE TenantId=@Acme AND MeetingTitle=N'Product Planning') INSERT INTO Reservations (TenantId,RoomId,UserId,MeetingTitle,Description,StartTime,EndTime,AttendeesCount,Status) VALUES (@Acme,@BlueRoom,@AcmeUser,N'Product Planning',N'วางแผน Product สำหรับไตรมาสถัดไป',DATEADD(HOUR,2,GETDATE()),DATEADD(HOUR,3,GETDATE()),5,'Approved');
GO
