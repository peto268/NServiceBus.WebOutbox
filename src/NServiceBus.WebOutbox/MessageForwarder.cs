using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus.Extensibility;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace NServiceBus.WebOutbox
{
	internal class MessageForwarder
	{
		private readonly string _destinationEndpointName;
		private readonly IRawEndpoint _destinationEndpoint;

		public MessageForwarder(string destinationEndpointName, IRawEndpoint destinationEndpoint)
		{
			_destinationEndpointName = destinationEndpointName;
			_destinationEndpoint = destinationEndpoint;
		}

		public async Task OnMessage(MessageContext context, IDispatchMessages _)
		{
			if (!context.Headers.TryGetValue(Headers.MessageIntent, out var messageIntentString)
			    || !Enum.TryParse(messageIntentString, true, out MessageIntentEnum messageIntent))
			{
				return;
			}

			var request = new OutgoingMessage(
				messageId: context.MessageId,
				headers: context.Headers,
				body: context.Body);

			if (!context.Headers.TryGetValue(WebOutboxHeaders.Destination, out var destination))
			{
				destination = _destinationEndpointName;
			}

			TransportOperation operation;
			switch (messageIntent)
			{
				case MessageIntentEnum.Send:
					operation = new TransportOperation(
						request,
						new UnicastAddressTag(destination));
					break;
				case MessageIntentEnum.Publish:

					if (!context.Headers.TryGetValue(Headers.EnclosedMessageTypes, out var enclosedMessageTypes))
					{
						throw new UnforwardableMessageException(
							"Message needs to have 'NServiceBus.EnclosedMessageTypes' header in order to be forwarded.");
					}

					var messageType = enclosedMessageTypes
						.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries).First();

					operation = new TransportOperation(
						request,
						new MulticastAddressTag(Type.GetType(messageType)));

					break;
				default:
					return;
			}

			var transaction = new TransportTransaction();
			
			// Participate in a tx scope if its active
			transaction.Set(Transaction.Current);

			await _destinationEndpoint.Dispatch(
					outgoingMessages: new TransportOperations(operation),
					transaction: transaction,
					context: new ContextBag())
				.ConfigureAwait(false);
		}
	}
}