using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;
using NServiceBus.WebOutbox;
using Shared;

namespace Web
{
	class Program
	{
		private const string SqlConnectionString =
			"Data Source=.\\SQL2019;Initial Catalog=WebOutboxSample;Trusted_Connection=True";

		static async Task Main()
		{
			var worker = await CreateWorkerEndpoint();
			var webOutbox = await CreateWebOutbox();

			DbTransaction currentTransaction = null;

			// ReSharper disable once AccessToModifiedClosure
			var webMessageSession = webOutbox.CreateMessageSession(() =>
				TransportTransactionFactory.CreateFromDbTransaction(currentTransaction));

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

				await using var conn = new SqlConnection(SqlConnectionString);
				await conn.OpenAsync();
				await using var tran = currentTransaction = await conn.BeginTransactionAsync();

				if (line.StartsWith("to "))
				{
					await webMessageSession.Send(line.Substring(3), new TestCommand { Text = $"From Web {line}" });
				}
				else
				{
					await webMessageSession.Send(new TestCommand { Text = $"From Web {line}" });
					await webMessageSession.Publish<ITestEvent>(e => e.Text = $"From Web {line}");
				}

				if (line == "rollback")
				{
					await currentTransaction.RollbackAsync();
					continue;
				}

				await currentTransaction.CommitAsync();
			}
			while (true);

			await webOutbox.Stop();
			await worker.Stop();
		}

		private static async Task<WebOutbox> CreateWebOutbox()
		{
			var webOutboxConfiguration = new WebOutboxConfiguration(
				outboxEndpointName: "Web",
				destinationEndpointName: "Worker",
				poisonMessageQueue: "poison");

			webOutboxConfiguration.ConfigureOutboxTransport<SqlServerTransport>(
				transport =>
				{
					transport.ConnectionString(SqlConnectionString);
				});

			webOutboxConfiguration.ConfigureDestinationTransport<LearningTransport>();

			webOutboxConfiguration.RouteToEndpoint(typeof(TestCommand), "Worker");

			webOutboxConfiguration.AutoCreateQueues();

			return await webOutboxConfiguration.StartOutbox();
		}

		private static async Task<IEndpointInstance> CreateWorkerEndpoint()
		{
			var workerConfiguration = new EndpointConfiguration("Worker");

			workerConfiguration.UseTransport<LearningTransport>();

			workerConfiguration.UsePersistence<InMemoryPersistence>();
			workerConfiguration.EnableInstallers();

			return await Endpoint.Start(workerConfiguration);
		}
	}
}
