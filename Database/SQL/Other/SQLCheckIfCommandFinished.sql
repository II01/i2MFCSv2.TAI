USE MFCS
UPDATE dbo.Command 
SET Status = 4
WHERE EXISTS (
	SELECT * FROM dbo.Command JOIN dbo.Place ON [Command].Material = [Place].Material
	WHERE Place=Target
	)