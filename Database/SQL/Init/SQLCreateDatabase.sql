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

use [MFCS]
INSERT INTO [PlaceID] values 
	('C101', 2, 0, 0),
	('T100', 2, 0, 0)

declare @r int
declare @rr int
declare @x int
declare @xx int
declare @y int
declare @z int
declare @loc nvarchar(20)
declare @heightclass int
set @r = 0
while(@r < 2) begin
	set @rr = (@r/2+1) * 10 + (@r % 2+1)
	set @r = @r + 1
	set @x=0
	while(@x < 26) begin
		set @x = @x + 1
		set @y=0
		while(@y < 10) begin
			set @y = @y + 1
			if @y=4 or @y>=9
				set @heightclass=2
			else 
				set @heightclass=1
			set @z=0
			while(@z < 1) begin
				set @z = @z + 1
				set @loc = 'W:' + RIGHT('00'+CAST(@rr AS VARCHAR(2)),2) + ':' + RIGHT('00'+CAST(@x AS VARCHAR(2)),2) + ':' + RIGHT('00'+CAST(@y AS VARCHAR(2)),2) + ':' + CAST(@z AS VARCHAR(1))
				INSERT INTO [PlaceID] values (@loc, @heightclass, 0, 0)
			end
		end
	end
end

delete from PlaceID
where ID like 'W:11:01:%'

