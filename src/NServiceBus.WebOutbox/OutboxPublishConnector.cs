using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Routing;

namespace NServiceBus.WebOutbox
{
	internal class OutboxPublishConnector : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
	{
		private readonly string _outboxEndpointName;

		public OutboxPublishConnector(string outboxEndpointName)
		{
			_outboxEndpointName = outboxEndpointName;
		}

		public override async Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
		{
			context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();

			var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(
				context.Message,
				new RoutingStrategy[]
				{
					new UnicastRoutingStrategy(_outboxEndpointName)
				},
				context);

			await stage(logicalMessageContext).ConfigureAwait(false);
		}
	}
}