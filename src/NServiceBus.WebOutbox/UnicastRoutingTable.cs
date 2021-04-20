using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Routing;

namespace NServiceBus.WebOutbox
{
	internal class UnicastRoutingTable
	{
		private readonly Dictionary<object, IList<RouteTableEntry>> _routeGroups = new Dictionary<object, IList<RouteTableEntry>>();
		private readonly object _updateLock = new object();
		private Dictionary<Type, UnicastRoute> _routeTable = new Dictionary<Type, UnicastRoute>();

		public UnicastRoute GetRouteFor(Type messageType)
		{
			return _routeTable.TryGetValue(messageType, out var unicastRoute)
				? unicastRoute
				: null;
		}

		public void AddOrReplaceRoutes(string sourceKey, IList<RouteTableEntry> entries)
		{
			lock (_updateLock)
			{
				_routeGroups[sourceKey] = entries;
				var newRouteTable = new Dictionary<Type, UnicastRoute>();
				foreach (var entry in _routeGroups.Values.SelectMany(g => g))
				{
					if (newRouteTable.ContainsKey(entry.MessageType))
					{
						throw new Exception($"Route for type {entry.MessageType.FullName} already exists.");
					}
					newRouteTable[entry.MessageType] = entry.Route;
				}
				_routeTable = newRouteTable;
			}
		}
	}
}