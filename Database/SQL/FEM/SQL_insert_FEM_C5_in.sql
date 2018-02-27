USE MFCS

declare @testcase int 

set @testcase = 2

if @testcase = 1 
	/* SSI1 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		('C501', 'T023', 11, 0, 'Crane'),
		('C501', 'W:52:06:1:1', 11, 0, 'Crane'),
		('C501', 'W:52:15:1:1', 11, 0, 'Crane'),
		('C501', 'T023', 11, 0, 'Crane')
else 
	/* SSI2 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		('C501', 'T321', 11, 0, 'Crane'),
		('C501', 'W:52:09:1:1', 11, 0, 'Crane'),
		('C501', 'W:52:19:1:1', 11, 0, 'Crane'),
		('C501', 'T321', 11, 0, 'Crane')

GO