﻿declare @tableName nvarchar(max) = @tablePrefix + 'OutboxData';

if exists
(
    select *
    from sys.objects
    where
        object_id = object_id(@tableName) and
        type in ('U')
)
begin
declare @dropTable nvarchar(max);
set @dropTable = 'drop table ' + @tableName;
exec(@dropTable);
end
