using NServiceBus.Persistence.Sql;

static class SagaDefinitionValidator
{
    internal static void ValidateSagaDefinition(string correlationProperty, string sagaName, string transitionalProperty, string tableSuffix)
    {
        if (correlationProperty != null && string.IsNullOrWhiteSpace(correlationProperty))
        {
            throw new ErrorsException($"The Saga '{sagaName}' has an empty string defined for CorrelationProperty.");
        }

        if (transitionalProperty != null && string.IsNullOrWhiteSpace(transitionalProperty))
        {
            throw new ErrorsException($"The Saga '{sagaName}' has an empty string defined for TransitionalCorrelationProperty.");
        }

        if (tableSuffix != null && string.IsNullOrWhiteSpace(tableSuffix))
        {
            throw new ErrorsException($"The Saga '{sagaName}' has an empty string defined for to TableSuffix.");
        }

        if (correlationProperty != null && correlationProperty == transitionalProperty)
        {
            throw new ErrorsException($"The Saga '{sagaName}' has CorrelationProperty and TransitionalCorrelationProperty the same. Member: {correlationProperty}");
        }

        if (correlationProperty == null && transitionalProperty != null)
        {
            throw new ErrorsException($"The Saga '{sagaName}' has null CorrelationProperty is null and non null TransitionalCorrelationProperty. If TransitionalCorrelationProperty is defined then CorrelationProperty must also be defined.");
        }
    }

}