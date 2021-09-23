using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace NServiceBus.WebOutbox
{
	public class WebOutboxConfiguration
	{
		private readonly IList<RouteTableEntry> _configRouteTableEntries = new List<RouteTableEntry>();
		private readonly UnicastRoutingTable _unicastRoutingTable = new UnicastRoutingTable();

		private readonly EndpointConfiguration _outboxEndpointConfiguration;
		private readonly List<Action<EndpointConfiguration>> _outboxEndpointConfigurationActions = new List<Action<EndpointConfiguration>>();

		private readonly RawEndpointConfiguration _forwarderEndpointConfiguration;

		private readonly string _destinationEndpointName;
		private readonly RawEndpointConfiguration _destinationEndpointConfiguration;

		private Func<MessageContext, IDispatchMessages, Task> _onMessage;

		public WebOutboxConfiguration(string outboxEndpointName, string destinationEndpointName, string poisonMessageQueue)
		{
			Guard.AgainstNullAndEmpty(outboxEndpointName, nameof(outboxEndpointName));
			Guard.AgainstNullAndEmpty(destinationEndpointName, nameof(destinationEndpointName));
			Guard.AgainstNullAndEmpty(poisonMessageQueue, nameof(poisonMessageQueue));

			_outboxEndpointConfiguration = new EndpointConfiguration(outboxEndpointName);

			_outboxEndpointConfiguration.Pipeline.Replace(
				"UnicastSendRouterConnector",
				new UnicastSendRouterConnector(_unicastRoutingTable, outboxEndpointName),
				"Routes all messages to the outbox endpoint");

			_outboxEndpointConfiguration.Pipeline.Replace(
				"OutgoingPhysicalToRoutingConnector",
				new OutgoingPhysicalToRoutingConnector(outboxEndpointName),
				"Routes all messages to the outbox endpoint");

			_outboxEndpointConfiguration.SendOnly();

			_forwarderEndpointConfiguration = RawEndpointConfiguration.Create(
				endpointName: outboxEndpointName,
				onMessage: (context, messages) => _onMessage?.Invoke(context, messages),
				poisonMessageQueue: poisonMessageQueue);

			_destinationEndpointName = destinationEndpointName;
			_destinationEndpointConfiguration = RawEndpointConfiguration.CreateSendOnly(destinationEndpointName);
		}

		public void ConfigureOutboxTransport<TOutbox>(
			Action<TransportExtensions<TOutbox>> configureOutboxTransport = null)
			where TOutbox : TransportDefinition, new()
		{
			var outboxTransport = _outboxEndpointConfiguration.UseTransport<TOutbox>();
			configureOutboxTransport?.Invoke(outboxTransport);

			var forwarderTransport = _forwarderEndpointConfiguration.UseTransport<TOutbox>();
			configureOutboxTransport?.Invoke(forwarderTransport);
			// Prevent distributed transactions with the destination transport
			forwarderTransport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);
		}

		public void ConfigureOutboxEndpoint(Action<EndpointConfiguration> configureOutboxEndpoint)
		{
			Guard.AgainstNull(configureOutboxEndpoint, nameof(configureOutboxEndpoint));
			_outboxEndpointConfigurationActions.Add(configureOutboxEndpoint);
		}

		public void ConfigureDestinationTransport<TDestination>(
			Action<TransportExtensions<TDestination>> configureDestinationTransport = null)
			where TDestination : TransportDefinition, new()
		{
			var transport = _destinationEndpointConfiguration.UseTransport<TDestination>();
			configureDestinationTransport?.Invoke(transport);
		}

		public void AutoCreateQueues()
		{
			_forwarderEndpointConfiguration.AutoCreateQueue();
			_destinationEndpointConfiguration.AutoCreateQueue();
		}

		public void RouteToEndpoint(Type messageType, string destination)
		{
			_configRouteTableEntries.Add(new RouteTableEntry(messageType, UnicastRoute.CreateFromEndpointName(destination)));
		}

		public void AddOrReplaceRoutes(string sourceKey, IList<RouteTableEntry> entries)
		{
			_unicastRoutingTable.AddOrReplaceRoutes(sourceKey, entries);
		}

		public async Task<WebOutbox> StartOutbox()
		{
			var destinationEndpoint = await RawEndpoint.Start(_destinationEndpointConfiguration).ConfigureAwait(false);

			// Setup the message forwarder
			var forwarder = new MessageForwarder(_destinationEndpointName, destinationEndpoint);
			_onMessage = forwarder.OnMessage;

			var forwarderEndpoint = await RawEndpoint.Start(_forwarderEndpointConfiguration).ConfigureAwait(false);

			foreach (var configAction in _outboxEndpointConfigurationActions)
			{
				configAction.Invoke(_outboxEndpointConfiguration);
			}

			_unicastRoutingTable.AddOrReplaceRoutes("EndpointConfiguration", _configRouteTableEntries);

			var outboxEndpoint = await Endpoint.Start(_outboxEndpointConfiguration).ConfigureAwait(false);

			return new WebOutbox(outboxEndpoint, forwarderEndpoint, destinationEndpoint);
		}
	}
}