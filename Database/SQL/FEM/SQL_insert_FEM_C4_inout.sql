USE MFCS

declare @testcase int 

set @testcase = 4

if @testcase = 1 
	/* SC1 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		('C401', 'T012', 11, 0, 'Crane'),
		('C401', 'T211', 11, 0, 'Crane'),
		('C401', 'T122', 11, 0, 'Crane'),
		('C401', 'T021', 11, 0, 'Crane'),
		('C401', 'T012', 11, 0, 'Crane')
else if @testcase = 2
	/* SC2 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		('C401', 'T012', 11, 0, 'Crane'),
		('C401', 'T211', 11, 0, 'Crane'),
		('C401', 'T222', 11, 0, 'Crane'),
		('C401', 'T021', 11, 0, 'Crane'),
		('C401', 'T012', 11, 0, 'Crane')
else if @testcase = 3
	/* SC3 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		('C401', 'T012', 11, 0, 'Crane'),
		('C401', 'T311', 11, 0, 'Crane'),
		('C401', 'T122', 11, 0, 'Crane'),
		('C401', 'T021', 11, 0, 'Crane'),
		('C401', 'T012', 11, 0, 'Crane')
else
	/* SC4 */
	INSERT INTO [SIMPLECOMMAND] (Unit, Source, Task, Status, Discrimator) VALUES
		('C401', 'T012', 11, 0, 'Crane'),
		('C401', 'T311', 11, 0, 'Crane'),
		('C401', 'T222', 11, 0, 'Crane'),
		('C401', 'T021', 11, 0, 'Crane'),
		('C401', 'T012', 11, 0, 'Crane')

	GO