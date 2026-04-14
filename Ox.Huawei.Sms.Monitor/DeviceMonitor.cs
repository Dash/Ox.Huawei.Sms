using Microsoft.Extensions.Options;
using Ox.Huawei.Sms.Monitor.Dispatchers;

namespace Ox.Huawei.Sms.Monitor
{
	/// <summary>
	/// Callback method for dispatching error messages
	/// </summary>
	/// <param name="error">Exception</param>
	/// <param name="cancellationToken"></param>
	public delegate Task DispatchError(Exception error, CancellationToken cancellationToken);

	/// <summary>
	/// This class handles monitoring logic for the Huawei USB dongle device's status
	/// </summary>
	/// <param name="client">API Client</param>
	/// <param name="dispatcher">Alert dispatchers</param>
	/// <param name="lifetime">Application lifetime (to allow shutdown on failure)</param>
	/// <param name="options">Configuration options</param>
	/// <param name="logger">Optional. Standard logger</param>
	public sealed class DeviceMonitor(
		IDeviceClient client,
		DispatcherBroker dispatcher,
		IHostApplicationLifetime lifetime,
		IOptions<DeviceMonitorOptions> options,
		ILogger<DeviceMonitor>? logger = null) : IHostedService, IDisposable, IAsyncDisposable
	{
		public const string NO_SIGNAL = "<-120dBm";

		private readonly IDeviceClient client = client ?? throw new ArgumentNullException(nameof(client));
		private readonly CancellationTokenSource cancellationToken = new();
		private readonly int pollInterval = options.Value.PollInterval * 1000;
		private readonly ILogger<DeviceMonitor>? log = logger;

		private readonly DispatchError failureAction = dispatcher.DispatchError;
		private readonly IHostApplicationLifetime applicationLifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));

		private Task? montiorTask;
		private bool disposedValue;

		/// <summary>
		/// Main polling mechanism
		/// </summary>
		private async Task Poll()
		{
			if(this.log?.IsEnabled(LogLevel.Debug) ?? false)
				this.log?.LogDebug("Polling signal status.");

			try
			{
				var status = await this.client.DeviceSignal();

				// Check 3G and 4G signal values
				// If all are empty, we've lost the network
				if (
					(String.IsNullOrEmpty(status?.rssi) || status?.rssi == NO_SIGNAL)
					&& String.IsNullOrEmpty(status?.rsrq)
					&& String.IsNullOrEmpty(status?.rsrp)
					)
				{
					this.log?.LogError("No cellular signal detected.");

					throw new IOException("USB Dongle has no cellular connection.");
				}
			}
			catch (TaskCanceledException exCancelled)        // Wrap timeouts to avoid conflicting with shutdown behaviour
			{
				throw new ApplicationException("Timeout calling modem API.", exCancelled);
			}
		}

		/// <summary>
		/// Manages the lifecycle of this class' monitor
		/// </summary>
		public async Task Monitor()
		{
			int failureCount = 0;

			if(this.log?.IsEnabled(LogLevel.Information) ?? false)
				this.log?.LogInformation("Device monitor starting.");

			await Task.Yield();

			while (!this.cancellationToken.IsCancellationRequested)
			{
				if(this.log?.IsEnabled(LogLevel.Debug) ?? false)
					this.log?.LogDebug("Device poll start.");

				try
				{

					await this.Poll();
					await Task.Delay(this.pollInterval, this.cancellationToken.Token);
					if (failureCount > 0 && (this.log?.IsEnabled(LogLevel.Information) ?? false))
						this.log?.LogInformation("Recovered from error state.");
					failureCount = 0;
				}
				catch (OperationCanceledException exCancelled)
				{
					if (this.log?.IsEnabled(LogLevel.Information) ?? false)
						this.log?.LogInformation(exCancelled, "Device monitor operation cancelling.");
					break;
				}
				catch (Exception ex)
				{
					this.log?.LogError(ex, "{Message}", ex.Message);
					if (++failureCount > 1)
					{
						this.log?.LogWarning("Error threshold exceeded, shutting down.");
						// Time to give up
						// Send notification of fatal error
						await this.failureAction(ex, CancellationToken.None);

						Environment.ExitCode = 1;   // Indicate failure
						this.applicationLifetime.StopApplication();
						break;
					}
					else
					{
						// Attempt a restart
						await this.client.DeviceRestart();

						await Task.Delay(this.pollInterval, this.cancellationToken.Token);
					}
				}
			}

			if (this.log?.IsEnabled(LogLevel.Information) ?? false)
				this.log?.LogInformation("Device monitor cancelled.");
		}

		/// <summary>
		/// Starts this service and returns immediately
		/// </summary>
		/// <param name="cancellationToken">Aborts startup (unused)</param>
		/// <returns>Completed Task</returns>
		public Task StartAsync(CancellationToken cancellationToken)
		{
			this.montiorTask = this.Monitor();  // This is a fire and forget, the task becomes orphaned
			return Task.CompletedTask;
		}

		/// <summary>
		/// Stops this service gracefully
		/// </summary>
		/// <param name="cancellationToken">Time's up for stopping, unused</param>
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			if (this.log?.IsEnabled(LogLevel.Information) ?? false)
				this.log?.LogInformation("Device monitor stopping.");

			await this.cancellationToken.CancelAsync();

			try
			{
				if (this.montiorTask != null)
					await this.montiorTask;
			}
			catch (TaskCanceledException)
			{
				// Swallow
			}
		}

		private void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					this.cancellationToken.Dispose();
				}

				this.disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		public ValueTask DisposeAsync()
		{
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
			return ValueTask.CompletedTask;
		}
	}

	/// <summary>
	/// Configuration options for <see cref="DeviceMonitor"/>
	/// </summary>
	public sealed class DeviceMonitorOptions
	{
		public int PollInterval { get; set; } = 300;
	}
}
