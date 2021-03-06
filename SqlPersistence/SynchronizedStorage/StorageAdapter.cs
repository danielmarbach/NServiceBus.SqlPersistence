using System.Data.SqlClient;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Transport;

class StorageAdapter : ISynchronizedStorageAdapter
{
    static readonly Task<CompletableSynchronizedStorageSession> EmptyResult = Task.FromResult((CompletableSynchronizedStorageSession)null);

    public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
    {
        var outboxTransaction = transaction as SqlOutboxTransaction;
        if (outboxTransaction != null)
        {
            CompletableSynchronizedStorageSession session = new StorageSession(outboxTransaction.Connection, outboxTransaction.Transaction, false);
            return Task.FromResult(session);
        }
        return EmptyResult;
    }

    public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
    {
        SqlConnection existingSqlConnection;
        SqlTransaction existingSqlTransaction;
        //SQL server transport in native TX mode
        if (transportTransaction.TryGet(out existingSqlConnection) && transportTransaction.TryGet(out existingSqlTransaction))
        {
            CompletableSynchronizedStorageSession session = new StorageSession(existingSqlConnection, existingSqlTransaction, false);
            return Task.FromResult(session);
        }
        //Other modes handled by creating a new session.
        return EmptyResult;
    }
}