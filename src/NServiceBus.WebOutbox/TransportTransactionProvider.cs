using System;
using NServiceBus.Transport;

namespace NServiceBus.WebOutbox
{
	internal class TransportTransactionProvider : ITransportTransactionProvider
	{
		private readonly Func<TransportTransaction> _transportTransactionFunc;

		public TransportTransaction TransportTransaction => _transportTransactionFunc();

		public TransportTransactionProvider(Func<TransportTransaction> transportTransactionFunc)
		{
			_transportTransactionFunc = transportTransactionFunc;
		}
	}
}