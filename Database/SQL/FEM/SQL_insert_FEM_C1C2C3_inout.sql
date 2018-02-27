USE MFCS

declare @crane int
declare @testcase int
declare @craneid nvarchar(20)
declare @in nvarchar(20)
declare @out nvarchar(20)
declare @p11 nvarchar(20)
declare @p12 nvarchar(20)
declare @p1r nvarchar(20)
declare @p21 nvarchar(20)
declare @p22 nvarchar(20)
declare @p2r nvarchar(20)

set @crane = 3
set @testcase = 4

set @craneid = 'C' + cast(@crane as varchar(1)) + '01'
set @p11 = 'W:' + cast(@crane as varchar(1)) + '1:12:6:1'
set @p12 = 'W:' + cast(@crane as varchar(1)) + '1:12:6:2'
set @p1r = 'W:' + cast(@crane as varchar(1)) + '1:12:5:2'
set @p21 = 'W:' + cast(@crane as varchar(1)) + '1:40:2:1'
set @p22 = 'W:' + cast(@crane as varchar(1)) + '1:40:2:2'
set @p2r = 'W:' + cast(@crane as varchar(1)) + '1:39:3:2'
set @in = 'T' + cast(@crane as varchar(1)) + '12'
if @crane = 3
	set @out = 'T' + cast(@crane as varchar(1)) + '21'
else
	set @out = @in

select @craneid, @in, @p11, @p22

if @testcase = 1
	/* CC1 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		(@craneid, @in, 11, 0, 'Crane'),
		(@craneid, @p11, 11, 0, 'Crane'),
		(@craneid, @p21, 11, 0, 'Crane'),
		(@craneid, @out, 11, 0, 'Crane'),
		(@craneid, @in, 11, 0, 'Crane')
else if @testcase = 2
	/* CC2 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		(@craneid, @in, 11, 0, 'Crane'),
		(@craneid, @p12, 11, 0, 'Crane'),
		(@craneid, @p22, 11, 0, 'Crane'),
		(@craneid, @out, 11, 0, 'Crane'),
		(@craneid, @in, 11, 0, 'Crane')
else if @testcase = 3
	/* CC3 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		(@craneid, @in, 11, 0, 'Crane'),
		(@craneid, @p11, 11, 0, 'Crane'),
		(@craneid, @p21, 11, 0, 'Crane'),
		(@craneid, @p2r, 11, 0, 'Crane'),
		(@craneid, @p22, 11, 0, 'Crane'),
		(@craneid, @out, 11, 0, 'Crane'),
		(@craneid, @in, 11, 0, 'Crane')
else
	/* CC4 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		(@craneid, @in, 11, 0, 'Crane'),
		(@craneid, @p22, 11, 0, 'Crane'),
		(@craneid, @p11, 11, 0, 'Crane'),
		(@craneid, @p1r, 11, 0, 'Crane'),
		(@craneid, @p12, 11, 0, 'Crane'),
		(@craneid, @out, 11, 0, 'Crane'),
		(@craneid, @in, 11, 0, 'Crane')

GO
