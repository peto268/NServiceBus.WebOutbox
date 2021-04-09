using System;
using System.Threading.Tasks;
using NServiceBus;

namespace Sample
{
	public class TestHandler : IHandleMessages<TestCommand>, IHandleMessages<ITestEvent>
	{
		public Task Handle(TestCommand message, IMessageHandlerContext context)
		{
			Console.WriteLine($"TestCommand: {message.Text}");
			return Task.CompletedTask;
		}

		public Task Handle(ITestEvent message, IMessageHandlerContext context)
		{
			Console.WriteLine($"ITestEvent: {message.Text}");
			return Task.CompletedTask;
		}
	}
}