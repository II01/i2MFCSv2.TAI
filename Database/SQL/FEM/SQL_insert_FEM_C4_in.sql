USE MFCS

declare @testcase int 

set @testcase = 2

if @testcase = 1 
	/* SI1 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		('C401', 'T012', 11, 0, 'Crane'),
		('C401', 'T211', 11, 0, 'Crane'),
		('C401', 'T012', 11, 0, 'Crane')
else
	/* SI2 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		('C401', 'T012', 11, 0, 'Crane'),
		('C401', 'T311', 11, 0, 'Crane'),
		('C401', 'T012', 11, 0, 'Crane')

GO