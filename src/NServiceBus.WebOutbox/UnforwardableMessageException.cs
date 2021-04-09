using System;

namespace NServiceBus.WebOutbox
{
	internal class UnforwardableMessageException : Exception
	{
		public UnforwardableMessageException(string message) : base(message)
		{
		}
	}
}