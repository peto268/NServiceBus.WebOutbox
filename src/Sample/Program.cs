using System;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.WebOutbox;

namespace Sample
{
	class Program
	{
		private const string SqlConnectionString =
			"Data Source=.\\SQL2019;Initial Catalog=WebOutboxSample;Trusted_Connection=True";

		static void Main()
		{
			MainAsync().GetAwaiter().GetResult();
		}

		static async Task MainAsync()
		{
			var worker = await CreateWorkerEndpoint();
			var web = await CreateWebEndpoint();

			Console.WriteLine("Type something to send messages");
			Console.WriteLine("Type \"rollback\" to simulate the send/rollback scenario");
			Console.WriteLine("Type \"exit\" to quit");

			do
			{
				var line = Console.ReadLine();
				if (line == null || line == "exit")
				{
					break;
				}

				using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

				await web.Send(new TestCommand {Text = $"From web {line}"});
				await web.Publish<ITestEvent>(e => e.Text = $"From web {line}");

				if (line == "rollback")
				{
					continue;
				}

				scope.Complete();
			}
			while (true);

			await web.Stop();
			await worker.Stop();
		}

		private static async Task<IEndpointInstance> CreateWebEndpoint()
		{
			var webOutboxConfiguration = new WebOutboxConfiguration<LearningTransport>(
				outboxEndpointName: "Web",
				configureOutboxTransport: transport =>
				{
					transport.ConnectionString(SqlConnectionString);
				},
				destinationEndpointName: "Worker",
				configureDestinationTransport: transport =>
				{
				},
				poisonMessageQueue: "poison");

			webOutboxConfiguration.AutoCreateQueue();

			return await webOutboxConfiguration.Start();
		}

		private static async Task<IEndpointInstance> CreateWorkerEndpoint()
		{
			var workerConfiguration = new EndpointConfiguration("Worker");

			var transport = workerConfiguration.UseTransport<LearningTransport>();

			var routing = transport.Routing();
			routing.RouteToEndpoint(typeof(Program).Assembly, "Worker");

			workerConfiguration.UsePersistence<InMemoryPersistence>();
			workerConfiguration.EnableInstallers();

			return await Endpoint.Start(workerConfiguration);
		}
	}
}
