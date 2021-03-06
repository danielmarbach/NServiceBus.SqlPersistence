﻿using NServiceBus.Persistence.Sql.ScriptBuilder;
using Resourcer;

static class ResourceReader
{
    public static string ReadResource(BuildSqlVariant sqlVariant, string prefix)
    {
        return Resource.AsStringUnChecked($"NServiceBus.Persistence.Sql.ScriptBuilder.{prefix}_{sqlVariant}.sql");
    }
}