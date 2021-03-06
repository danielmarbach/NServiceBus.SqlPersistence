﻿using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class SqlVariantReader
{
    public static IEnumerable<BuildSqlVariant> Read(ModuleDefinition moduleDefinition)
    {
        var assemblyCustomAttributes = moduleDefinition.Assembly.CustomAttributes;
        var customAttribute = assemblyCustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "NServiceBus.Persistence.Sql.SqlPersistenceSettingsAttribute");
        if (customAttribute == null)
        {
            yield return BuildSqlVariant.MsSqlServer;
            yield return BuildSqlVariant.MySql;
            yield break;
        }
        var arguments = customAttribute.ConstructorArguments;
        var msSqlServerScripts = (bool) arguments[0].Value;
        var mySqlScripts = (bool) arguments[1].Value;
        if (msSqlServerScripts)
        {
            yield return BuildSqlVariant.MsSqlServer;
        }
        if (mySqlScripts)
        {
            yield return BuildSqlVariant.MySql;
        }
        if (!msSqlServerScripts && !mySqlScripts)
        {
            throw new ErrorsException("Must define either MsSqlServerScripts, MySqlScripts, or both. Add a [SqlPersistenceSettingsAttribute] to the assembly.");
        }
    }
}