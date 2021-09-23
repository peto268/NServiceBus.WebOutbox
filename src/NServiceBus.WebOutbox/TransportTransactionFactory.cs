using System;
using System.Data.Common;
using NServiceBus.Transport;

namespace NServiceBus.WebOutbox
{
	public static class TransportTransactionFactory
	{
		public static TransportTransaction CreateFromDbTransaction(DbTransaction transaction)
		{
			if (transaction == null)
			{
				return null;
			}

			if (transaction.Connection == null)
			{
				throw new ArgumentException($"Transaction has no connection associated, transaction is no longer valid");
			}

			var transportTransaction = new TransportTransaction();
			transportTransaction.Set("System.Data.SqlClient.SqlConnection", transaction.Connection);
			transportTransaction.Set("System.Data.SqlClient.SqlTransaction", transaction);
			return transportTransaction;
		}
	}
}