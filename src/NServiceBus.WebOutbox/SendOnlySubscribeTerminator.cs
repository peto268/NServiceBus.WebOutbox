using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.WebOutbox
{
	internal class SendOnlySubscribeTerminator : PipelineTerminator<ISubscribeContext>
	{
		protected override Task Terminate(ISubscribeContext context)
		{
			throw new InvalidOperationException("Outbox endpoint cannot subscribe to events.");
		}
	}
}