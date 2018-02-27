use mfcs
select cast(Time as Date) as Day, datepart(HOUR,Time) as Hour, count(*) as Number from dbo.Place
GROUP BY cast(Time as Date), datepart(HOUR, Time)

select cast(Time as Date) as Day, datepart(DAY,Time) as Hour, count(*) as Number from dbo.Place
GROUP BY cast(Time as Date), datepart(DAY, Time)
