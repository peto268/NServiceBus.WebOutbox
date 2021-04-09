using System;
using System.Threading.Tasks;
using NServiceBus.Raw;

namespace NServiceBus.WebOutbox
{
	internal class WebOutboxEndpoint : IEndpointInstance
	{
		private readonly string _outboxEndpointName;
		private readonly IEndpointInstance _outboxEndpoint;
		private readonly IStoppableRawEndpoint _forwarderEndpoint;
		private readonly IStoppableRawEndpoint _destinationEndpoint;

		public WebOutboxEndpoint(string outboxEndpointName, IEndpointInstance outboxEndpoint,
			IStoppableRawEndpoint forwarderEndpoint, IStoppableRawEndpoint destinationEndpoint)
		{
			_outboxEndpointName = outboxEndpointName;
			_outboxEndpoint = outboxEndpoint;
			_forwarderEndpoint = forwarderEndpoint;
			_destinationEndpoint = destinationEndpoint;
		}

		public Task Send(object message, SendOptions options)
		{
			options.SetDestination(_outboxEndpointName);
			return _outboxEndpoint.Send(message, options);
		}

		public Task Send<T>(Action<T> messageConstructor, SendOptions options)
		{
			options.SetDestination(_outboxEndpointName);
			return _outboxEndpoint.Send(messageConstructor, options);
		}

		public Task Publish(object message, PublishOptions options)
		{
			return _outboxEndpoint.Publish(message, options);
		}

		public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
		{
			return _outboxEndpoint.Publish(messageConstructor, publishOptions);
		}

		public Task Subscribe(Type eventType, SubscribeOptions options)
		{
			throw new NotSupportedException();
		}

		public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
		{
			throw new NotSupportedException();
		}

		public async Task Stop()
		{
			await _outboxEndpoint.Stop().ConfigureAwait(false);
			await _forwarderEndpoint.Stop().ConfigureAwait(false);
			await _destinationEndpoint.Stop().ConfigureAwait(false);
		}
	}
}