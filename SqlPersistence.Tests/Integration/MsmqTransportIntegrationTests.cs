﻿using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;
using NUnit.Framework;

[TestFixture]
public class MsmqTransportIntegrationTests : IDisposable
{
    static string connectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=sqlpersistencetests;Integrated Security=True";
    static ManualResetEvent ManualResetEvent = new ManualResetEvent(false);
    string endpointName = "MsmqTransportIntegration";
    BuildSqlVariant sqlVariant = BuildSqlVariant.MsSqlServer;
    SqlConnection dbConnection;
    SagaDefinition sagaDefinition;

    public MsmqTransportIntegrationTests()
    {
        dbConnection = new SqlConnection(connectionString);
        dbConnection.Open();
        sagaDefinition = new SagaDefinition(
            tableSuffix: nameof(Saga1),
            name: nameof(Saga1),
            correlationProperty: new CorrelationProperty
            (
                name: nameof(Saga1.SagaData.StartId),
                type: CorrelationPropertyType.Guid
            )
        );
    }

    [SetUp]
    public void Setup()
    {
        MsmqQueueDeletion.DeleteQueuesForEndpoint(endpointName);
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaDefinition, sqlVariant), nameof(MsmqTransportIntegrationTests));
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildCreateScript(sagaDefinition, sqlVariant), nameof(MsmqTransportIntegrationTests));
        dbConnection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlVariant), nameof(MsmqTransportIntegrationTests));
        dbConnection.ExecuteCommand(TimeoutScriptBuilder.BuildCreateScript(sqlVariant), nameof(MsmqTransportIntegrationTests));
    }

    [TearDown]
    public void TearDown()
    {
        MsmqQueueDeletion.DeleteQueuesForEndpoint(endpointName);
        dbConnection.ExecuteCommand(SagaScriptBuilder.BuildDropScript(sagaDefinition, sqlVariant), nameof(MsmqTransportIntegrationTests));
        dbConnection.ExecuteCommand(TimeoutScriptBuilder.BuildDropScript(sqlVariant), nameof(MsmqTransportIntegrationTests));
    }

    [Test]
    [TestCase(TransportTransactionMode.TransactionScope)]
    [TestCase(TransportTransactionMode.SendsAtomicWithReceive)]
    [TestCase(TransportTransactionMode.ReceiveOnly)]
    [TestCase(TransportTransactionMode.None)]
    public async Task Write(TransportTransactionMode transactionMode)
    {
        var endpointConfiguration = EndpointConfigBuilder.BuildEndpoint(nameof(MsmqTransportIntegrationTests));
        var typesToScan = TypeScanner.NestedTypes<MsmqTransportIntegrationTests>();
        endpointConfiguration.SetTypesToScan(typesToScan);
        var transport = endpointConfiguration.UseTransport<MsmqTransport>();
        transport.Transactions(transactionMode);
        var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
        persistence.ConnectionBuilder(() => new SqlConnection(connectionString));
        persistence.DisableInstaller();

        var endpoint = await Endpoint.Start(endpointConfiguration);
        var startSagaMessage = new StartSagaMessage
        {
            StartId = Guid.NewGuid()
        };
        await endpoint.SendLocal(startSagaMessage);
        ManualResetEvent.WaitOne();
        await endpoint.Stop();
    }

    public class StartSagaMessage : IMessage
    {
        public Guid StartId { get; set; }
    }

    public class TimeoutMessage : IMessage
    {
    }

    [SqlSaga(
         correlationProperty: nameof(SagaData.StartId)
     )]
    public class Saga1 : SqlSaga<Saga1.SagaData>,
        IAmStartedByMessages<StartSagaMessage>,
        IHandleTimeouts<TimeoutMessage>
    {
        public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
        {
            return RequestTimeout<TimeoutMessage>(context, TimeSpan.FromMilliseconds(100));
        }

        public Task Timeout(TimeoutMessage state, IMessageHandlerContext context)
        {
            MarkAsComplete();
            ManualResetEvent.Set();
            return Task.CompletedTask;
        }

        public class SagaData : ContainSagaData
        {
            public Guid StartId { get; set; }
        }

        protected override void ConfigureMapping(MessagePropertyMapper<SagaData> mapper)
        {
            mapper.MapMessage<StartSagaMessage>(message => message.StartId);
        }

    }

    class MessageToReply : IMessage
    {
    }

    public void Dispose()
    {
        dbConnection?.Dispose();
    }
}