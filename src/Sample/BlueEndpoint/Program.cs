using System;
using System.Threading.Tasks;
using NServiceBus;

namespace BlueEndpoint
{
	class Program
	{
		static void Main()
		{
			MainAsync().GetAwaiter().GetResult();
		}

		static async Task MainAsync()
		{
			var worker = await CreateWorkerEndpoint();

			Console.WriteLine("Type \"exit\" to exit");

			do
			{
				var line = Console.ReadLine();
				if (line == null || line == "exit")
				{
					break;
				}
			}
			while (true);

			await worker.Stop();
		}

		private static async Task<IEndpointInstance> CreateWorkerEndpoint()
		{
			var workerConfiguration = new EndpointConfiguration("BlueEndpoint");

			workerConfiguration.UseTransport<LearningTransport>();

			workerConfiguration.UsePersistence<InMemoryPersistence>();
			workerConfiguration.EnableInstallers();

			return await Endpoint.Start(workerConfiguration);
		}
	}
}
