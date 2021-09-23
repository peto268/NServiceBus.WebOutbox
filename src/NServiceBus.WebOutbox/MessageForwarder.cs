using System;
using System.Linq;
using System.Threading.Tasks;
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

			if (context.Headers.ContainsKey(Headers.OriginatingEndpoint))
			{
				context.Headers[Headers.OriginatingEndpoint] = _destinationEndpointName;
			}

			TransportOperation operation;
			switch (messageIntent)
			{
				case MessageIntentEnum.Send:
					operation = new TransportOperation(
						request,
						new UnicastAddressTag(GetDestination(context)));
					break;
				case MessageIntentEnum.Publish:
					operation = new TransportOperation(
						request,
						new MulticastAddressTag(GetMessageType(context)));
					break;
				default:
					return;
			}

			await _destinationEndpoint.Dispatch(
					outgoingMessages: new TransportOperations(operation),
					transaction: new TransportTransaction(),
					context: new ContextBag())
				.ConfigureAwait(false);
		}

		private static string GetDestination(MessageContext context)
		{
			if (!context.Headers.TryGetValue(WebOutboxHeaders.Destination, out var destination))
			{
				throw new UnforwardableMessageException(
					$"Message needs to have '{WebOutboxHeaders.Destination}' header in order to be forwarded.");
			}

			return destination;
		}

		private static Type GetMessageType(MessageContext context)
		{
			if (!context.Headers.TryGetValue(Headers.EnclosedMessageTypes, out var enclosedMessageTypes))
			{
				throw new UnforwardableMessageException(
					$"Message needs to have '{Headers.EnclosedMessageTypes}' header in order to be forwarded.");
			}

			var messageType = enclosedMessageTypes
				.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries).First();

			return Type.GetType(messageType);
		}
	}
}