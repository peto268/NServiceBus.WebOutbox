using NServiceBus.Transport;

namespace NServiceBus.WebOutbox
{
	public interface ITransportTransactionProvider
	{
		TransportTransaction TransportTransaction { get; }
	}
}