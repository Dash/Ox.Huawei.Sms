using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ox.Huawei.Sms.Messages;
using Ox.Huawei.Sms.Monitor.Tests.Doubles;
using static Ox.Huawei.Sms.Monitor.Tests.Doubles.MockDispatcher;

[assembly: Parallelize()]

namespace Ox.Huawei.Sms.Monitor.Tests
{
	[TestClass()]
	public class SmsMonitorTests
	{
		private static SmsMonitor NewSmsMonitor(
			Doubles.MockSmsClient? smsClient = null,
			FakeApplicationLifetime? appLifetime = null,
			EventHandler<SmsEventArgs>? smsCallback = null
			)
		{
			var opts = Options.Create(new SmsMonitorOptions()
			{
				PollInterval = 0,
			});

			var mock = new MockDispatcher();
			mock.SmsDispatched += smsCallback;
			var dispatcher = new Dispatchers.DispatcherBroker(dispatchers: [mock]);


			return new SmsMonitor(
				smsClient ?? new Doubles.MockSmsClient(),
				dispatcher,
				opts,
				appLifetime ?? new Doubles.FakeApplicationLifetime(),
				NullLogger<SmsMonitor>.Instance);
		}

		[TestMethod()]
		[Description("Checks monitor starts up correctly.")]
		public async Task StartStopAsync_Successful()
		{
			var clt = new MockSmsClient();
			var tks = new TaskCompletionSource();
			clt.SmsListMock = (filter) =>
			{
				tks.SetResult();
				return [];
			};
			var m = NewSmsMonitor(clt);
			await m.StartAsync(CancellationToken.None);
			await Task.WhenAny(tks.Task, Task.Delay(1000, this.TestContext.CancellationToken));
			Assert.IsTrue(tks.Task.IsCompleted, "Monitor did not start in a timely fashion.");
			await m.StopAsync(CancellationToken.None);
			Assert.IsFalse(m.IsRunning, "Monitor still running.");
		}

		[TestMethod()]
		[Description("Checks that monitor can recover after single error.")]
		public async Task Monitor_ErrorWithRecovery()
		{

			using var cts = new CancellationTokenSource();
			var clt = new MockSmsClient();
			var tks = new TaskCompletionSource();

			int callCount = 0;

			clt.SmsListMock = (filter) =>
			{
				if (callCount++ == 0)
				{
					throw new NotFiniteNumberException();
				}
				else if (callCount == 2)
				{
					tks.SetResult();
				}
				return [];
			};
			var m = NewSmsMonitor(clt);
			await m.StartAsync(cts.Token);
			await Task.WhenAny(tks.Task, Task.Delay(1000, this.TestContext.CancellationToken));
			Assert.IsTrue(tks.Task.IsCompleted, "Monitor didn't recover in a timely fashion.");
			Assert.AreEqual(0, Environment.ExitCode, "Environment exit code incorrect.");
			Assert.IsTrue(m.IsRunning, "Monitor task not operational");
		}

		[TestMethod()]
		[Description("Checks that monitor bails after too many errors.")]
		public async Task Monitor_FatalErrorRetries()
		{
			using var cts = new CancellationTokenSource();
			var clt = new MockSmsClient();
			var tks = new TaskCompletionSource();
			var lt = new FakeApplicationLifetime();

			lt.StopApplicationCalled += (sender, e) => tks.SetResult();

			clt.SmsListMock = (filter) => throw new NotFiniteNumberException();
			var m = NewSmsMonitor(clt, lt);
			await m.StartAsync(cts.Token);
			await Task.WhenAny(tks.Task, Task.Delay(1000, this.TestContext.CancellationToken));
			Assert.IsTrue(tks.Task.IsCompleted, "Monitor error failed to call application stop in a timely fashion.");
			Assert.AreEqual(1, Environment.ExitCode, "Environment exit code not set");
			await m.StopAsync(CancellationToken.None);
			Environment.ExitCode = 0;
		}

		[TestMethod()]
		[Description("Checks Http timeout errors will shutdown app cleanly")]
		public async Task Monitor_ClientTimeout()
		{
			var clt = new MockSmsClient
			{
				SmsListMock = (filter) =>
					// This gets thrown by the HttpClient on a timeout
					throw new TaskCanceledException("Foobar")
			};

			var tks = new TaskCompletionSource();
			var lt = new FakeApplicationLifetime();
			lt.StopApplicationCalled += (sender, args) => tks.SetResult();

			var m = NewSmsMonitor(clt, lt);
			await m.StartAsync(CancellationToken.None);
			await Task.WhenAny(tks.Task, Task.Delay(1000, this.TestContext.CancellationToken));
			Assert.IsTrue(tks.Task.IsCompleted, "Persistent client time out did not stop application in a timely fashion.");
			Assert.AreEqual(1, Environment.ExitCode, "Environment exit code not set.");
			await m.StopAsync(CancellationToken.None);
			Environment.ExitCode = 0;

		}

		[TestMethod()]
		[Description("Checks monitor can handle multiple messages.")]
		public async Task Monitor_MultipleMessages()
		{
			var clt = new MockSmsClient();
			int sentCount = 0;
			var listStub = new List<SmsListMessage>() {
					new()
					{
						Index = 1
					},
					new() {
						Index = 2
					}
				};
			var tks = new TaskCompletionSource();
			clt.SmsListMock = (filter) => listStub;

			var m = NewSmsMonitor(clt, smsCallback: (sender, arg) =>
			{
				if (++sentCount == listStub.Count)
					tks.SetResult();
			});

			await m.StartAsync(CancellationToken.None);

			await Task.WhenAny(tks.Task, Task.Delay(1000, this.TestContext.CancellationToken));
			Assert.IsTrue(tks.Task.IsCompleted, "Monitor did not complete in a timely fashion.");
			await m.StopAsync(CancellationToken.None);

			Assert.IsFalse(m.IsRunning, "Monitor still running.");

		}

		public TestContext TestContext { get; set; }
	}
}