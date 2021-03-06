using System;
using System.Data.Common;
using System.Data.SqlClient;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class SqlServerTimeoutPersisterTests : TimeoutPersisterTests
{
    string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=sqlpersistencetests;Integrated Security=True";
    public SqlServerTimeoutPersisterTests() : base(BuildSqlVariant.MsSqlServer)
    {
    }

    protected override Func<DbConnection> GetConnection()
    {
        return () => new SqlConnection(connectionString);
    }
}