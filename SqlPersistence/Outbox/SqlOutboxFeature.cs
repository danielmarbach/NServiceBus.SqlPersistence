﻿using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

class SqlOutboxFeature : Feature
{
    SqlOutboxFeature()
    {
        DependsOn<Outbox>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Settings.EnableFeature<StorageType.Outbox>();
        var settings = context.Settings;
        var connectionBuilder = settings.GetConnectionBuilder<StorageType.Outbox>();
        var sqlVariant = settings.GetSqlVariant();
        var endpointName = settings.GetTablePrefix<StorageType.Outbox>();
        var outboxPersister = new OutboxPersister(sqlVariant, connectionBuilder, endpointName);
        context.Container.ConfigureComponent(b => outboxPersister, DependencyLifecycle.InstancePerCall);
    }
}