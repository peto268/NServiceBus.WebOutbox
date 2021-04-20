using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Unicast.Queuing;

namespace NServiceBus.WebOutbox
{
	internal class UnicastSendRouterConnector : StageConnector<IOutgoingSendContext, IOutgoingLogicalMessageContext>
	{
		private readonly UnicastRoutingTable _unicastRoutingTable;
		private readonly string _outboxEndpointName;

		public UnicastSendRouterConnector(UnicastRoutingTable unicastRoutingTable, string outboxEndpointName)
		{
			_unicastRoutingTable = unicastRoutingTable;
			_outboxEndpointName = outboxEndpointName;
		}

		public override async Task Invoke(IOutgoingSendContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
		{
			context.Headers[Headers.MessageIntent] = MessageIntentEnum.Send.ToString();

			if (!context.Headers.ContainsKey(WebOutboxHeaders.Destination))
			{
				context.Headers[WebOutboxHeaders.Destination] = GetDestination(context);
			}

			var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(
				context.Message,
				new[]
				{
					new UnicastRoutingStrategy(_outboxEndpointName)
				},
				context);

			try
			{
				await stage(logicalMessageContext).ConfigureAwait(false);
			}
			catch (QueueNotFoundException ex)
			{
				throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({context.Message.MessageType}) in the routing section of the transport configuration. It may also be the case that the given queue hasn't been created yet, or has been deleted.", ex);
			}
		}

		private string GetDestination(IOutgoingSendContext context)
		{
			context.Headers.TryGetValue(WebOutboxHeaders.Destination, out var destination);

			if (destination != null)
			{
				return destination;
			}

			var route = _unicastRoutingTable.GetRouteFor(context.Message.MessageType);

			if (route == null)
			{
				throw new Exception($"No destination specified for message: {context.Message.MessageType}");
			}

			if (route.PhysicalAddress != null)
			{
				return route.PhysicalAddress;
			}
			if (route.Instance != null)
			{
				return route.Instance.Endpoint;
			}

			return route.Endpoint;
		}
	}
}