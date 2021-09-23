using System;

namespace NServiceBus.WebOutbox
{
	internal static class Guard
	{
		public static void AgainstNull(object value, string argumentName)
		{
			if (value == null)
			{
				throw new ArgumentNullException(argumentName);
			}
		}

		public static void AgainstNullAndEmpty(string value, string argumentName)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				throw new ArgumentException("Value cannot be null or white space.", argumentName);
			}
		}
	}
}