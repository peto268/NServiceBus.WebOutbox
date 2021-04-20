using NServiceBus;

namespace Shared
{
	public class TestCommand : ICommand
	{
		public string Text { get; set; }
	}

	public interface ITestEvent : IEvent
	{
		string Text { get; set; }
	}
}