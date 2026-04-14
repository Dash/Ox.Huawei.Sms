using Ox.Huawei.Sms.Messages;

namespace Ox.Huawei.Sms.Monitor.Dispatchers
{
	public class DispatcherBroker : IDispatcher
	{
		private IDispatcher[] Dispatchers { get; set; }

		public DispatcherBroker(IEnumerable<IDispatcher> dispatchers)
		{
			this.Dispatchers = dispatchers?.ToArray() ?? throw new ArgumentNullException(nameof(dispatchers), "At least one dispatcher is required.");
			if(this.Dispatchers.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(dispatchers), "At least one dispatcher is required.");
		}

		public async Task DispatchError(Exception ex, CancellationToken cancellationToken)
		{
			Task[] tasks = new Task[this.Dispatchers.Length];

			for (int i = 0; i < this.Dispatchers.Length; i++)
			{
				tasks[i] = this.Dispatchers[i].DispatchError(ex, cancellationToken);
			}

			await Task.WhenAll(tasks);
		}

		public async Task DispatchSms(SmsListMessage sms, CancellationToken cancellationToken)
		{
			Task[] tasks = new Task[this.Dispatchers.Length];

			for (int i = 0; i < this.Dispatchers.Length; i++)
			{
				tasks[i] = this.Dispatchers[i].DispatchSms(sms, cancellationToken);
			}

			await Task.WhenAll(tasks);
		}

		public override string ToString() => 
			$"Dispatcher Broker:{Environment.NewLine}{String.Join(Environment.NewLine, this.Dispatchers.Select(d => d.GetType().Name))}";
	}

	public sealed class DispatcherOptions
	{
		public List<object>? Dispatchers { get; set; }
	}
}