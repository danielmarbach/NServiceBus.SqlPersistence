﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Persistence.Sql;

    public class When_using_ReplyToOriginator : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_set_Reply_as_messageintent()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new InitiateRequestingSaga
                {
                    SomeCorrelationId = Guid.NewGuid()
                })))
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual(MessageIntentEnum.Reply, context.Intent);
        }

        public class Context : ScenarioContext
        {
            public MessageIntentEnum Intent { get; set; }
            public bool Done { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>());
            }
            
            public class RequestingSaga : SqlSaga<RequestingSaga.RequestingSagaData>,
                IAmStartedByMessages<InitiateRequestingSaga>,
                IHandleMessages<AnotherRequest>
            {
                public Task Handle(InitiateRequestingSaga message, IMessageHandlerContext context)
                {
                    Data.CorrIdForResponse = message.SomeCorrelationId; //wont be needed in the future

                    return context.SendLocal(new AnotherRequest
                    {
                        SomeCorrelationId = Data.CorrIdForResponse //wont be needed in the future
                    });
                }

                public async Task Handle(AnotherRequest message, IMessageHandlerContext context)
                {
                    await ReplyToOriginator(context, new MyReplyToOriginator());
                    MarkAsComplete();
                }

                protected override string CorrelationPropertyName => nameof(RequestingSagaData.CorrIdForResponse);

                protected override void ConfigureMapping(IMessagePropertyMapper mapper)
                {
                    mapper.ConfigureMapping<InitiateRequestingSaga>(m => m.SomeCorrelationId);
                    mapper.ConfigureMapping<AnotherRequest>(m => m.SomeCorrelationId);
                }

                public class RequestingSagaData : ContainSagaData
                {
                    public virtual Guid CorrIdForResponse { get; set; } //wont be needed in the future
                }
            }

            class MyReplyToOriginatorHandler : IHandleMessages<MyReplyToOriginator>
            {
                public Context TestContext { get; set; }

                public Task Handle(MyReplyToOriginator message, IMessageHandlerContext context)
                {
                    TestContext.Intent = (MessageIntentEnum) Enum.Parse(typeof(MessageIntentEnum), context.MessageHeaders[Headers.MessageIntent]);
                    TestContext.Done = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class InitiateRequestingSaga : ICommand
        {
            public Guid SomeCorrelationId { get; set; }
        }

        public class AnotherRequest : ICommand
        {
            public Guid SomeCorrelationId { get; set; }
        }

        public class MyReplyToOriginator : IMessage
        {
            public Guid SomeCorrelationId { get; set; }
        }
    }
}