DECLARE @TableMap TABLE(
	[TableName] NVARCHAR(MAX) NOT NULL,
	[Checksum] INT
)

INSERT INTO @TableMap
		( [TableName], [Checksum] )
EXEC sp_MSForEachTable 'SELECT
					"?",
					CHECKSUM_AGG(BINARY_CHECKSUM(*)) 
				FROM 
					? WITH (NOLOCK);';

SELECT 
	[TableName],
	[Checksum]
FROM 
	@TableMap
ORDER BY
	[TableName]