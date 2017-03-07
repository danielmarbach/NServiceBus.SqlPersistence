﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

partial class SagaPersister
{
    public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaType = context.GetSagaType();
        var result = await Get<TSagaData>(sagaId, session, sagaType);
        return SetConcurrency(result, context);
    }


    internal async Task<Concurrency<TSagaData>> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, Type sagaType)
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData), sagaType);
        var sqlSession = session.SqlPersistenceSession();
        using (var command = sqlSession.Connection.CreateCommand())
        {
            command.CommandText = sagaInfo.GetBySagaIdCommand;
            command.Transaction = sqlSession.Transaction;
            command.AddParameter("Id", sagaId);
            return await GetSagaData<TSagaData>(command, sagaInfo);
        }
    }


    public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaType = context.GetSagaType();
        var result = await Get<TSagaData>(propertyName, propertyValue, session, sagaType);
        return SetConcurrency(result, context);
    }

    internal async Task<Concurrency<TSagaData>> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, Type sagaType)
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData), sagaType);

        ValidatePropertyName<TSagaData>(propertyName, sagaInfo);
        var commandText = sagaInfo.GetByCorrelationPropertyCommand;
        var sqlSession = session.SqlPersistenceSession();

        using (var command = sqlSession.Connection.CreateCommand())
        {
            command.CommandText = commandText;
            command.Transaction = sqlSession.Transaction;
            command.AddParameter("propertyValue", propertyValue);
            return await GetSagaData<TSagaData>(command, sagaInfo);
        }
    }


    static async Task<Concurrency<TSagaData>> GetSagaData<TSagaData>(DbCommand command, RuntimeSagaInfo sagaInfo)
        where TSagaData : IContainSagaData
    {
        // to avoid loading into memory SequentialAccess is required which means each fields needs to be accessed
        using (var dataReader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess))
        {
            if (!await dataReader.ReadAsync())
            {
                return default(Concurrency<TSagaData>);
            }

            var id = await dataReader.GetGuidAsync(0);
            var sagaTypeVersionString = await dataReader.GetFieldValueAsync<string>(1);
            var sagaTypeVersion = Version.Parse(sagaTypeVersionString);
            var concurrency = await dataReader.GetFieldValueAsync<int>(2);
            string originator;
            string originalMessageId;
            using (var textReader = dataReader.GetTextReader(3))
            {
                var metadata = Serializer.Deserialize<Dictionary<string, string>>(textReader);
                metadata.TryGetValue("Originator", out originator);
                metadata.TryGetValue("OriginalMessageId", out originalMessageId);
            }
            using (var textReader = dataReader.GetTextReader(4))
            {
                var sagaData = sagaInfo.FromString<TSagaData>(textReader, sagaTypeVersion);
                sagaData.Id = id;
                sagaData.Originator = originator;
                sagaData.OriginalMessageId = originalMessageId;
                return new Concurrency<TSagaData>(sagaData, concurrency);
            }
        }
    }

    static void ValidatePropertyName<TSagaData>(string propertyName, RuntimeSagaInfo sagaInfo) where TSagaData : IContainSagaData
    {
        if (!sagaInfo.HasCorrelationProperty)
        {
            throw new Exception($"Cannot retrieve a {typeof(TSagaData).FullName} using property \'{propertyName}\'. The saga has no correlation property.");
        }
        if (propertyName != sagaInfo.CorrelationProperty)
        {
            throw new Exception($"Cannot retrieve a {typeof(TSagaData).FullName} using property \'{propertyName}\'. Can only be retrieve using the correlation property '{sagaInfo.CorrelationProperty}'");
        }
    }
}