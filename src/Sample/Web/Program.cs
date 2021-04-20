﻿using System;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.WebOutbox;
using Shared;

namespace Web
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
			Console.WriteLine("Type \"to BlueEndpoint\" to simulate sending a message to a specific endpoint");
			Console.WriteLine("Type \"exit\" to exit");

			do
			{
				var line = Console.ReadLine();
				if (line == null || line == "exit")
				{
					break;
				}

				using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

				if (line.StartsWith("to "))
				{
					await web.Send(line.Substring(3), new TestCommand {Text = $"From Web {line}"});
				}
				else
				{
					await web.Send(new TestCommand {Text = $"From Web {line}"});
					await web.Publish<ITestEvent>(e => e.Text = $"From Web {line}");
				}

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
			routing.RouteToEndpoint(typeof(TestCommand), "Worker");

			workerConfiguration.UsePersistence<InMemoryPersistence>();
			workerConfiguration.EnableInstallers();

			return await Endpoint.Start(workerConfiguration);
		}
	}
}