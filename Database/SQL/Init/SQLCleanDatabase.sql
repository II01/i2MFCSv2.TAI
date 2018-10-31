﻿delete from [Alarm]
delete from [Event]
delete from [Movement]
delete from [Command]
delete from [SimpleCommand]
delete from [HistCommand]
delete from [HistSimpleCommand]
delete from [Place]
delete from [MaterialID] 

select
	(select count(*) from [Alarm]) as tAlarm,
	(select count(*) from [Event]) as tEvent,
	(select count(*) from [Movement]) as tMovement,
	(select count(*) from [Command]) as tCommand,
	(select count(*) from [SimpleCommand]) as tSimpleCommand,
	(select count(*) from [HistCommand]) as tCommand,
	(select count(*) from [HistSimpleCommand]) as tSimpleCommand,
	(select count(*) from [Place]) as tPlace,
	(select count(*) from [MaterialID]) as tMaterialID

