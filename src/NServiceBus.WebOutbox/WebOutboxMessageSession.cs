using System;
using System.Threading.Tasks;
using NServiceBus.Extensibility;

namespace NServiceBus.WebOutbox
{
	internal class WebOutboxMessageSession : IMessageSession
	{
		private readonly IMessageSession _outboxMessageSession;
		private readonly ITransportTransactionProvider _transportTransactionProvider;

		public WebOutboxMessageSession(IMessageSession outboxMessageSession,
			ITransportTransactionProvider transportTransactionProvider)
		{
			_outboxMessageSession = outboxMessageSession;
			_transportTransactionProvider = transportTransactionProvider;
		}

		public Task Send(object message, SendOptions options)
		{
			SetupDestination(options);
			SetupTransportTransaction(options);
			return _outboxMessageSession.Send(message, options);
		}

		public Task Send<T>(Action<T> messageConstructor, SendOptions options)
		{
			SetupDestination(options);
			SetupTransportTransaction(options);
			return _outboxMessageSession.Send(messageConstructor, options);
		}

		public Task Publish(object message, PublishOptions options)
		{
			SetupTransportTransaction(options);
			return _outboxMessageSession.Publish(message, options);
		}

		public Task Publish<T>(Action<T> messageConstructor, PublishOptions options)
		{
			SetupTransportTransaction(options);
			return _outboxMessageSession.Publish(messageConstructor, options);
		}

		public Task Subscribe(Type eventType, SubscribeOptions options)
		{
			throw new InvalidOperationException("Outbox endpoint cannot subscribe to events.");
		}

		public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
		{
			throw new InvalidOperationException("Outbox endpoint cannot unsubscribe from events.");
		}

		private static void SetupDestination(SendOptions options)
		{
			var destination = options.GetDestination();
			if (destination != null)
			{
				options.SetHeader("NServiceBus.WebOutbox.Destination", destination);
			}
		}

		private void SetupTransportTransaction(ExtendableOptions options)
		{
			var transaction = _transportTransactionProvider?.TransportTransaction;
			if (transaction != null)
			{
				options.GetExtensions().Set(transaction);
			}
		}
	}
}