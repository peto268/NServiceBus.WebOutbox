using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.WebOutbox
{
	internal class SendOnlyUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
	{
		protected override Task Terminate(IUnsubscribeContext context)
		{
			throw new InvalidOperationException("Outbox endpoint cannot unsubscribe from events.");
		}
	}
}