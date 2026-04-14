using Microsoft.Extensions.Hosting;

namespace Ox.Huawei.Sms.Monitor.Tests.Doubles
{
	internal class FakeApplicationLifetime : IHostApplicationLifetime
	{
		public CancellationToken ApplicationStarted { get; set; }

		public CancellationToken ApplicationStopping { get; set; }

		public CancellationToken ApplicationStopped { get; set; }

		public void StopApplication() => this.StopApplicationCalled?.Invoke(this, EventArgs.Empty);

		public event EventHandler? StopApplicationCalled;
	}
}
