using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace NServiceBus.WebOutbox
{
	public class WebOutboxConfiguration<TDestination> : WebOutboxConfiguration<SqlServerTransport, TDestination>
		where TDestination : TransportDefinition, new()
	{
		public WebOutboxConfiguration(string outboxEndpointName,
			Action<TransportExtensions<SqlServerTransport>> configureOutboxTransport,
			string destinationEndpointName,
			Action<TransportExtensions<TDestination>> configureDestinationTransport,
			string poisonMessageQueue)
			: base(outboxEndpointName, configureOutboxTransport,
				destinationEndpointName, configureDestinationTransport,
				poisonMessageQueue)
		{
		}
	}

	public class WebOutboxConfiguration<TOutbox, TDestination>
		where TOutbox : TransportDefinition, new()
		where TDestination : TransportDefinition, new()
	{
		private readonly IList<RouteTableEntry> _configRouteTableEntries;
		private readonly UnicastRoutingTable _unicastRoutingTable;

		private readonly EndpointConfiguration _outboxEndpointConfiguration;

		private readonly RawEndpointConfiguration _forwarderEndpointConfiguration;

		private readonly string _destinationEndpointName;
		private readonly RawEndpointConfiguration _destinationEndpointConfiguration;

		private Func<MessageContext, IDispatchMessages, Task> _onMessage;

		private Action<EndpointConfiguration> _configureOutboxEndpoint;

		public WebOutboxConfiguration(string outboxEndpointName,
			Action<TransportExtensions<TOutbox>> configureOutboxTransport,
			string destinationEndpointName,
			Action<TransportExtensions<TDestination>> configureDestinationTransport,
			string poisonMessageQueue)
		{
			_configRouteTableEntries = new List<RouteTableEntry>();
			_unicastRoutingTable = new UnicastRoutingTable();

			_outboxEndpointConfiguration = new EndpointConfiguration(outboxEndpointName);

			configureOutboxTransport(_outboxEndpointConfiguration.UseTransport<TOutbox>());

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

			var transportExtensions = _forwarderEndpointConfiguration.UseTransport<TOutbox>();
			configureOutboxTransport(transportExtensions);
			// Prevent distributed transactions with the destination transport
			transportExtensions.Transactions(TransportTransactionMode.SendsAtomicWithReceive);

			_destinationEndpointName = destinationEndpointName;
			_destinationEndpointConfiguration = RawEndpointConfiguration.CreateSendOnly(destinationEndpointName);

			configureDestinationTransport(_destinationEndpointConfiguration.UseTransport<TDestination>());
		}

		public void ConfigureOutboxEndpoint(Action<EndpointConfiguration> configureOutboxEndpoint)
		{
			_configureOutboxEndpoint = configureOutboxEndpoint;
		}

		public void AutoCreateQueue()
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

		public async Task<IEndpointInstance> Start()
		{
			var destinationEndpoint = await RawEndpoint.Start(_destinationEndpointConfiguration).ConfigureAwait(false);

			// Setup the message forwarder
			var forwarder = new MessageForwarder(_destinationEndpointName, destinationEndpoint);
			_onMessage = forwarder.OnMessage;

			var forwarderEndpoint = await RawEndpoint.Start(_forwarderEndpointConfiguration).ConfigureAwait(false);

			_configureOutboxEndpoint?.Invoke(_outboxEndpointConfiguration);

			_unicastRoutingTable.AddOrReplaceRoutes("EndpointConfiguration", _configRouteTableEntries);

			var outboxEndpoint = await Endpoint.Start(_outboxEndpointConfiguration).ConfigureAwait(false);

			return new WebOutboxEndpoint(outboxEndpoint, forwarderEndpoint, destinationEndpoint);
		}
	}
}