USE MFCS

declare @testcase int 

set @testcase = 2

if @testcase = 1
	/* SO1 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		('C401', 'T021', 11, 0, 'Crane'),
		('C401', 'T122', 11, 0, 'Crane'),
		('C401', 'T021', 11, 0, 'Crane')
else
	/* SO2 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		('C401', 'T021', 11, 0, 'Crane'),
		('C401', 'T222', 11, 0, 'Crane'),
		('C401', 'T021', 11, 0, 'Crane')
GO