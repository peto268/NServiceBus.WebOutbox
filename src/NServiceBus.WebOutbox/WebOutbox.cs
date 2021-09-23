using System;
using System.Threading.Tasks;
using NServiceBus.Raw;
using NServiceBus.Transport;

namespace NServiceBus.WebOutbox
{
	public class WebOutbox
	{
		private readonly IEndpointInstance _outboxEndpoint;
		private readonly IStoppableRawEndpoint _forwarderEndpoint;
		private readonly IStoppableRawEndpoint _destinationEndpoint;

		internal WebOutbox(IEndpointInstance outboxEndpoint,
			IStoppableRawEndpoint forwarderEndpoint, IStoppableRawEndpoint destinationEndpoint)
		{
			_outboxEndpoint = outboxEndpoint;
			_forwarderEndpoint = forwarderEndpoint;
			_destinationEndpoint = destinationEndpoint;
		}

		public IMessageSession CreateMessageSession()
		{
			return new WebOutboxMessageSession(_outboxEndpoint, null);
		}

		public IMessageSession CreateMessageSession(ITransportTransactionProvider transportTransactionProvider)
		{
			Guard.AgainstNull(transportTransactionProvider, nameof(transportTransactionProvider));
			return new WebOutboxMessageSession(_outboxEndpoint, transportTransactionProvider);
		}

		public IMessageSession CreateMessageSession(Func<TransportTransaction> transportTransactionFunc)
		{
			Guard.AgainstNull(transportTransactionFunc, nameof(transportTransactionFunc));
			return new WebOutboxMessageSession(_outboxEndpoint, new TransportTransactionProvider(transportTransactionFunc));
		}

		public async Task Stop()
		{
			await _outboxEndpoint.Stop().ConfigureAwait(false);
			await _forwarderEndpoint.Stop().ConfigureAwait(false);
			await _destinationEndpoint.Stop().ConfigureAwait(false);
		}
	}
}