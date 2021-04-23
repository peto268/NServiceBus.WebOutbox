using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace NServiceBus.WebOutbox
{
	internal class OutgoingPhysicalToRoutingConnector : StageConnector<IOutgoingPhysicalMessageContext, IRoutingContext>
	{
		private readonly string _outboxEndpointName;

		public OutgoingPhysicalToRoutingConnector(string outboxEndpointName)
		{
			_outboxEndpointName = outboxEndpointName;
		}

		public override async Task Invoke(IOutgoingPhysicalMessageContext context, Func<IRoutingContext, Task> stage)
		{
			var message = new OutgoingMessage(context.MessageId, context.Headers, context.Body);

			var routingContext = this.CreateRoutingContext(
				message,
				new RoutingStrategy[]
				{
					new UnicastRoutingStrategy(_outboxEndpointName)
				},
				context);

			await stage(routingContext).ConfigureAwait(false);
		}
	}
}