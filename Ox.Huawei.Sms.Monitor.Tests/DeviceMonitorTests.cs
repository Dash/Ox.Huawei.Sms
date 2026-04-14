using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ox.Huawei.Sms.Monitor.Dispatchers;
using Ox.Huawei.Sms.Monitor.Tests.Doubles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Ox.Huawei.Sms.Monitor.Tests
{
	[TestClass()]
	public sealed class DeviceMonitorTests
	{
		public TestContext TestContext { get; set; }
		private static DeviceMonitor NewDeviceMonitor(
			IDeviceClient? client = null,
			IDispatcher? dispatcher = null
			)
		{
			var mockDispatcher = dispatcher ?? new MockDispatcher();

			return new DeviceMonitor(
				client ?? new MockDeviceClient(),
				new Dispatchers.DispatcherBroker(dispatchers: [mockDispatcher]),
				new Doubles.FakeApplicationLifetime(),
				Options.Create<DeviceMonitorOptions>(new()
				{
					PollInterval = 0
				}),
				NullLogger<DeviceMonitor>.Instance
			);
		}

		/// <summary>
		/// Checks the monitor system kicks in when started
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task StartStopAsync_Succesful()
		{
			var client = new MockDeviceClient();
			TaskCompletionSource tcs = new();
			client.DeviceSignalMock += () =>
			{
				tcs.SetResult();
				return new();
			};

			bool error = false;
			var dispatcher = new MockDispatcher();
			dispatcher.ErrorDispatched += (_, _) => error = true;


			var monitor = NewDeviceMonitor(client);

			await monitor.StartAsync(this.TestContext.CancellationToken);

			await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1), this.TestContext.CancellationToken);

			Assert.IsTrue(tcs.Task.IsCompletedSuccessfully);

			await monitor.StopAsync(this.TestContext.CancellationToken).WaitAsync(TimeSpan.FromSeconds(1));

			Assert.IsFalse(error);
		}

		/// <summary>
		/// Checks that in a low-signal event the device is restarted
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task LowSignal_Failure()
		{
			var client = new MockDeviceClient();
			TaskCompletionSource<Exception> tcs = new();
			TaskCompletionSource tcsRestart = new();

			// Callback to provide poor signal
			client.DeviceSignalMock += () =>
			{
				Debug.WriteLine("Providing no signal result");
				return new Messages.DeviceSignal()
				{
					rssi = DeviceMonitor.NO_SIGNAL
				};
			};

			// Monitor for a device restart request
			client.DeviceRestartMock += () =>
			{
				Debug.WriteLine("Mocking device restart.");
				tcsRestart.SetResult();
				return true;
			};

			var dispatcher = new MockDispatcher();

			// Monitor for error message being raised
			dispatcher.ErrorDispatched += (_, e) =>
			{
				tcs.SetResult(e.GetException());
			};


			var monitor = NewDeviceMonitor(client: client, dispatcher: dispatcher);

			// Start monitor
			await monitor.StartAsync(this.TestContext.CancellationToken);

			// Wait for restart request
			await tcsRestart.Task.WaitAsync(TimeSpan.FromSeconds(1), this.TestContext.CancellationToken);

			Assert.IsTrue(tcsRestart.Task.IsCompletedSuccessfully, "Restart never requested.");

			// Wait for abandoned alert
			await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1), this.TestContext.CancellationToken);

			Assert.IsTrue(tcs.Task.IsCompletedSuccessfully, "Final error not raised.");

			await monitor.StopAsync(this.TestContext.CancellationToken).WaitAsync(TimeSpan.FromSeconds(1));
		}

		/// <summary>
		/// Checks that in a low-signal event the device is restarted and recovers
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task LowSignal_Recovery()
		{
			var client = new MockDeviceClient();
			TaskCompletionSource<Exception> tcs = new();
			TaskCompletionSource tcsRestart = new();

			// Callback to provide poor signal
			bool sendBadSignal = true;
			client.DeviceSignalMock += () =>
			{
				if (sendBadSignal)
				{
					Debug.WriteLine("Providing no signal result");
					sendBadSignal = false;
					return new Messages.DeviceSignal()
					{
						rssi = DeviceMonitor.NO_SIGNAL
					};
				}
				else
				{
					Debug.WriteLine("Providing good signal result");
					return new Messages.DeviceSignal()
					{
						rssi = "good",
						rsrq = "good",
						rsrp = "good"
					};
				}
			};

			// Monitor for a device restart request
			client.DeviceRestartMock += () =>
			{
				Debug.WriteLine("Mocking device restart.");
				tcsRestart.SetResult();
				return true;
			};

			var dispatcher = new MockDispatcher();

			// Monitor for error message being raised
			dispatcher.ErrorDispatched += (_, e) =>
			{
				tcs.SetResult(e.GetException());
			};

			var monitor = NewDeviceMonitor(client: client, dispatcher: dispatcher);

			// Start monitor
			await monitor.StartAsync(this.TestContext.CancellationToken);

			// Wait for restart request
			await tcsRestart.Task.WaitAsync(TimeSpan.FromSeconds(1), this.TestContext.CancellationToken);

			Assert.IsTrue(tcsRestart.Task.IsCompletedSuccessfully, "Restart never requested.");

			// Check that alert doesn't get raised.
			await Assert.ThrowsExactlyAsync<System.TimeoutException>(async () => await tcs.Task.WaitAsync(TimeSpan.FromSeconds(0.1), this.TestContext.CancellationToken));

			Assert.IsFalse(tcs.Task.IsCompleted, "Final error incorrectly raised.");

			await monitor.StopAsync(this.TestContext.CancellationToken).WaitAsync(TimeSpan.FromSeconds(1));
		}
	}
}
