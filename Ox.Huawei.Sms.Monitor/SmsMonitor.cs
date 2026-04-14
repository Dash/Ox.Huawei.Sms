using Microsoft.Extensions.Options;
using Ox.Huawei.Sms.Messages;
using Ox.Huawei.Sms.Monitor.Dispatchers;

namespace Ox.Huawei.Sms.Monitor
{
	/// <summary>
	/// Polls device for new messages
	/// </summary>
	public sealed class SmsMonitor(
		ISmsClient client,
		DispatcherBroker dispatcher,
		IOptions<SmsMonitorOptions> options,
		IHostApplicationLifetime lifetime,
		ILogger<SmsMonitor>? logger = null
		) : IHostedService
	{
		private readonly ISmsClient client = client ?? throw new ArgumentNullException(nameof(client));
		private readonly ILogger<SmsMonitor>? log = logger;
		private readonly DispatcherBroker dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
		private readonly int pollInterval = options.Value.PollInterval * 1000;
		private readonly bool deleteMessages = options.Value.DeleteAfterDispatch;
		private CancellationTokenSource cts = new();
		private readonly IHostApplicationLifetime applicationLifetime = lifetime;
		private Task? monitorTask;
		private readonly List<Task> tasks = new(1);

		/// <summary>
		/// Main polling mechanism of the dongle
		/// </summary>
		/// <remarks>
		/// This will check for any unread messages, dispatch them to the handler, and then mark them as read/delete.
		/// 
		/// It creates a new Task for each poll batch, which allows for graceful shutdown by awaiting it.
		/// </remarks>
		/// <returns></returns>
		private async Task Poll()
		{
			this.cts.Token.ThrowIfCancellationRequested();

			IList<SmsListMessage> list;
			this.tasks.Clear();

			// Get unread messages
			try
			{
				list = await this.client.SmsList(sms => sms.Smstat == 0);
			}
			catch (TaskCanceledException exCancelled)       // Wrap timeouts to avoid conflicting with shutdown behaviour
			{
				throw new ApplicationException("Timeout calling modem API.", exCancelled);
			}

			if (list.Any())
			{
				if (this.log?.IsEnabled(LogLevel.Information) ?? false)
					this.log?.LogInformation("{Count} new messages.", list.Count);

				if (this.cts.Token.IsCancellationRequested) return;

				foreach (var msg in list)
				{
					// Bail if cancelled, safe to do so before we mark as completed
					// But we won't throw out as there may be tasks currently in flight, we just won't add to them
					if (this.cts.IsCancellationRequested) break;

					if (this.log?.IsEnabled(LogLevel.Debug) ?? false)
						this.log?.LogDebug("Dispatching {Index}.", msg.Index);

					// Dispatch message with continuation to delete
					this.tasks.Add(this.dispatcher.DispatchSms(msg, this.cts.Token).ContinueWith(async (prev) => await this.Complete(prev, msg.Index)).Unwrap());
				}

				// Wait for this batch to complete
				await Task.WhenAll(this.tasks);
			}
		}

		/// <summary>
		/// Performs a complete action against the message after it has been dispatched (delete or mark read)
		/// </summary>
		/// <param name="dispatch">Dispatch task</param>
		/// <param name="id">Message id to complete</param>
		private async Task Complete(Task dispatch, int id)
		{
			await dispatch; // Allow for exception unwrapping

			if (this.log?.IsEnabled(LogLevel.Information) ?? false)
				this.log?.LogInformation("Completing message {Id}.", id);

			if (this.deleteMessages)
				await this.client.Delete(id);
			else
				await this.client.MarkRead(id);
		}

		/// <summary>
		/// Manages the periodic monitoring of for new SMS messages
		/// </summary>
		/// <remarks>
		/// Will bail when <see cref="cts"/> is triggered.
		/// </remarks>
		public async Task Monitor()
		{
			int failureCount = 0;

			this.log?.LogInformation($"SMS Monitor startup{Environment.NewLine}{this.dispatcher}");

			while (!this.cts.IsCancellationRequested)
			{
				this.log?.LogDebug("Sms poll start.");
				try
				{
					await this.Poll();
					await Task.Delay(this.pollInterval, this.cts.Token);
					if (failureCount > 0)
						this.log?.LogInformation("Recovered from error state.");
					failureCount = 0;
				}
				catch (OperationCanceledException exCancelled)
				{
					this.log?.LogInformation(exCancelled, "SMS Monitor cancelling.");
					break;
				}
				catch (Exception ex)
				{
					this.log?.LogError(ex, "{Message}", ex.Message);
					if (failureCount++ > 1)
					{
						this.log?.LogWarning("Error threshold exceeded, shutting down.");
						// Send error when fatal
						await this.dispatcher.DispatchError(ex, CancellationToken.None);

						Environment.ExitCode = 1;   // Indicate failure
						this.applicationLifetime.StopApplication();
						break;
					}
					else
					{
						await Task.Yield();
						await Task.Delay(this.pollInterval, this.cts.Token);
					}
				}
			}

			this.log?.LogInformation("SMS Monitor cancelled.");
		}

		/// <summary>
		/// Starts this background service and completes immediately
		/// </summary>
		/// <param name="cancellationToken">Aborts startup (no action)</param>
		/// <returns>Completed Task</returns>
		public Task StartAsync(CancellationToken cancellationToken)
		{
			this.cts = new();
			this.monitorTask = this.Monitor();
			return Task.CompletedTask;
		}

		/// <summary>
		/// Stops this background service
		/// </summary>
		/// <param name="cancellationToken">Used to signal time is up, not used</param>
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			this.log?.LogInformation("Stopping SMS Monitor");
			await this.cts.CancelAsync();

			try
			{
				if (this.monitorTask != null)
					await this.monitorTask;
			}
			catch (TaskCanceledException) { }

			this.cts.Dispose();
		}

		public bool IsRunning => !this.monitorTask?.IsCompleted ?? false;
	}

	/// <summary>
	/// Options for <see cref="SmsMonitor"/>
	/// </summary>
	public sealed class SmsMonitorOptions
	{
		/// <summary>
		/// Seconds between each poll for new messages
		/// </summary>
		public int PollInterval { get; set; } = 30;
		/// <summary>
		/// Whether to delete SMS' from the modem, or just mark them as read (default)
		/// </summary>
		public bool DeleteAfterDispatch { get; set; } = false;
	}

	/// <summary>
	/// Defines the callback method for dispatching SMS'
	/// </summary>
	/// <param name="sms">Message to send</param>
	/// <param name="cancellationToken">Abort token</param>
	public delegate Task DispatchSms(SmsListMessage sms, CancellationToken cancellationToken);
}
