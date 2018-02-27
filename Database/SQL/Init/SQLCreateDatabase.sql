USE MFCS

IF object_id('dbo.Numbers') IS NOT NULL
	DROP TABLE dbo.Numbers
GO

IF object_id('dbo.Event') IS NOT NULL
	DROP TABLE dbo.Event
GO

IF object_id('dbo.Movement') IS NOT NULL
	DROP TABLE dbo.Movement
GO

IF object_id('dbo.SimpleCraneCommand') IS NOT NULL
	DROP TABLE dbo.SimpleCraneCommand
GO


IF object_id('dbo.SimpleCommand') IS NOT NULL
	DROP TABLE dbo.SimpleCommand
GO

IF object_id('dbo.Command') IS NOT NULL
	DROP TABLE dbo.Command
GO


IF object_id('dbo.Place') IS NOT NULL
	DROP TABLE dbo.Place
GO


IF object_id('dbo.PlaceID') IS NOT NULL
	DROP TABLE dbo.PlaceID 
GO

IF object_id('dbo.MaterialID') IS NOT NULL
	DROP TABLE dbo.MaterialID
GO


if OBJECT_ID('dbo.SwitchLanguage') is not null
	DROP PROCEDURE [dbo].[SwitchLanguage]

if OBJECT_ID('dbo.Alarm') is not null 
	DROP TABLE [dbo].[Alarm]
	GO

if OBJECT_ID('dbo.AlarmDetail') is not null 
	DROP TABLE [dbo].[AlarmDetail]
	GO

if OBJECT_ID('dbo.Text') is not null 
	DROP table [dbo].[Text]
	GO

if OBJECT_ID('dbo.User') is not null 
	DROP table [dbo].[User]
	GO


CREATE TABLE [dbo].[PlaceID]
(
	ID NVARCHAR(20) PRIMARY KEY,
	Size INT NOT NULL,
	Blocked BIT NOT NULL,
	Reserved BIT NOT NULL
)
GO

CREATE Table [dbo].[MaterialID]
(
	ID INT PRIMARY KEY,
	Size INT NOT NULL,
	Weight INT NOT NULL 
)
GO

CREATE Table [dbo].[Place]
(
	Place NVARCHAR(20) NOT NULL, 
	Material INT NOT NULL,
	Time DATETIME NOT NULL DEFAULT GETDATE(),

	CONSTRAINT PK_material PRIMARY KEY (Material),
	INDEX IX_Place NONCLUSTERED (Place),
---	CONSTRAINT PK_place PRIMARY KEY (Place),
	CONSTRAINT FK_place_PlaceID FOREIGN KEY (Place) REFERENCES dbo.PlaceID (ID),  
	CONSTRAINT FK_material_MatetrialID FOREIGN KEY (Material) REFERENCES dbo.MaterialID (ID)
	--- CONSTRAINT  
)
GO

CREATE TABLE [dbo].[Command]
(
	ID INT IDENTITY PRIMARY KEY,
	WMS_ID INT NOT NULL, 
	Task INT NOT NULL,
	Material INT FOREIGN KEY (Material) REFERENCES [dbo].MaterialID(ID), 
	Source NVARCHAR(20) FOREIGN KEY (Source) REFERENCES [dbo].PlaceID(ID),
	Target NVARCHAR(20)  FOREIGN KEY (Target) REFERENCES [dbo].PlaceID(ID),
	Segment NVARCHAR(20),
	CommandID INT,
	Priority INT NOT NULL,
	Info NVARCHAR(50), 
	Status INT NOT NULL,
	Reason INT,
	Discrimator NVARCHAR(50) DEFAULT('Material'),
	Time DATETIME DEFAULT GETDATE()

	INDEX IX_Material NONCLUSTERED (Material), 
	INDEX IX_Source NONCLUSTERED (Source), 
	INDEX IX_Target NONCLUSTERED (Target)

)
GO

CREATE TABLE [dbo].[SimpleCommand]
(
	ID INT IDENTITY PRIMARY KEY,
	Command_ID INT FOREIGN KEY (Command_ID) REFERENCES [dbo].Command(ID) ON DELETE CASCADE,
	Unit NVARCHAR(20),
	Material INT FOREIGN KEY (Material) REFERENCES [dbo].MaterialID(ID), 
	Source NVARCHAR(20) FOREIGN KEY (Source) REFERENCES [dbo].PlaceID(ID),
	Target NVARCHAR(20) FOREIGN KEY (Target) REFERENCES [dbo].PlaceID(ID),
	Task INT NOT NULL,
	Segment NVARCHAR(20), 
	CancelID INT,
	Status INT NOT NULL,
	Reason INT,
	Discrimator NVARCHAR(50) NOT NULL DEFAULT('Crane'),
	Time DATETIME DEFAULT GETDATE() NOT NULL	 

	INDEX IX_Unit NONCLUSTERED (Unit), 
	INDEX IX_Material NONCLUSTERED (Material), 
	INDEX IX_Source NONCLUSTERED (Source),
	INDEX IX_Target NONCLUSTERED (Target)
)
GO


CREATE TABLE [dbo].[Movement]
(
	ID INT IDENTITY PRIMARY KEY, 
	Material INT NOT NULL,
	Position NVARCHAR(20) NOT NULL,
	Task INT,
	Time DATETIME NOT NULL DEFAULT GETDATE()

	CONSTRAINT FK_Material_MaterialID FOREIGN KEY (Material) REFERENCES [dbo].MaterialID(ID), 
	INDEX IX_Material NONCLUSTERED (Material)

)
GO

CREATE TABLE [dbo].[Event]
(
	ID INT IDENTITY PRIMARY KEY,
	Severity INT NOT NULL, 
	Type INT NOT NULL, 
	Text NVARCHAR(300) NOT NULL,
	[Time] DATETIME NOT NULL DEFAULT GETDATE()
)
	

CREATE TABLE [dbo].[Alarm]
(
	[ID] INT NOT NULL IDENTITY, 
    [Unit] NVARCHAR(50) NOT NULL, 
    [AlarmID] NVARCHAR(50) NOT NULL, 
    [Status] INT NOT NULL, 
    [Severity] INT NOT NULL, 
    [ArrivedTime] DATETIME NOT NULL, 
    [AckTime] DATETIME NULL, 
    [RemovedTime] DATETIME NULL,
	CONSTRAINT [PK_Alarm] PRIMARY KEY  ([ID])
)

GO

CREATE TABLE [dbo].[User]
(
	[ID] INT NOT NULL IDENTITY, 
    [User] NVARCHAR(50) NOT NULL, 
    [Password] NVARCHAR(50) NOT NULL, 
    [AccessLevel] INT NOT NULL, 
)

GO


--- prepare table with numbers for future use	 
--- SELECT TOP (1000) n = CONVERT(INT, ROW_NUMBER() OVER (ORDER BY s1.[object_id]))
--- INTO dbo.Numbers
--- FROM sys.all_objects AS s1 CROSS JOIN sys.all_objects AS s2
--- OPTION (MAXDOP 1);
---
--- GO

--- create user ---
INSERT INTO [User] values 
	('admin', 'admin', 2)

GO

--- create locations ---

INSERT INTO [PlaceID] values 
	('C101', 1, 0, 0),
	('C102', 1, 0, 0),
	('C201', 1, 0, 0),
	('C202', 1, 0, 0),
	('C301', 1, 0, 0),
	('T013', 1, 0, 0),
	('T014', 1, 0, 0),
	('T015', 1, 0, 0),
	('T021', 1, 0, 0),
	('T022', 1, 0, 0),
	('T023', 1, 0, 0),
	('T024', 1, 0, 0),
	('T025', 1, 0, 0),
	('T031', 1, 0, 0),
	('T032', 1, 0, 0),
	('T033', 1, 0, 0),
	('T034', 1, 0, 0),
	('T035', 1, 0, 0),
	('T036', 1, 0, 0),
	('T037', 1, 0, 0),
	('T038', 1, 0, 0),
	('T111', 1, 0, 0),
	('T112', 1, 0, 0),
	('T113', 1, 0, 0),
	('T114', 1, 0, 0),
	('T115', 1, 0, 0),
	('T121', 1, 0, 0),
	('T122', 1, 0, 0),
	('T123', 1, 0, 0),
	('T124', 1, 0, 0),
	('T125', 1, 0, 0),
	('T211', 1, 0, 0),
	('T212', 1, 0, 0),
	('T213', 1, 0, 0),
	('T214', 1, 0, 0),
	('T215', 1, 0, 0),
	('T221', 1, 0, 0),
	('T222', 1, 0, 0),
	('T223', 1, 0, 0),
	('T224', 1, 0, 0),
	('T225', 1, 0, 0),
	('T041', 1, 0, 0),
	('T042', 1, 0, 0)

declare @r int
declare @rr int
declare @x int
declare @xx int
declare @y int
declare @z int
declare @loc nvarchar(20)
set @r = 0
while(@r < 4) begin
	set @rr = (@r/2+1) * 10 + (@r % 2+1)
	set @r = @r + 1
	set @x=0
	while(@x < 126) begin
		set @x = @x + 1
		set @y=0
		while(@y < 9) begin
			set @y = @y + 1
			set @z=0
			while(@z < 2) begin
				set @z = @z + 1
				set @loc = 'W:' + RIGHT('00'+CAST(@rr AS VARCHAR(2)),2) + ':' + RIGHT('000'+CAST(@x AS VARCHAR(3)),3) + ':' + CAST(@y AS VARCHAR(1)) + ':' + CAST(@z AS VARCHAR(1))
				INSERT INTO [PlaceID] values (@loc, 1, 0, 0)
			end
		end
	end
end
set @x=0
while(@x < 5) begin
	set @x = @x + 1
	set @xx = 0
	while(@xx < 4) begin
		set @xx = @xx + 1
		set @loc = 'W:32:0' + CAST(@x AS VARCHAR(1)) + CAST(@xx as VARCHAR(1)) + ':1:1'
		INSERT INTO [PlaceID] values (@loc, 999, 0, 0)
	end 
end

GO
