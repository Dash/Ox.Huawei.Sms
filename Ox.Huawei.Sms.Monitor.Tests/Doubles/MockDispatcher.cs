using Ox.Huawei.Sms.Messages;
using Ox.Huawei.Sms.Monitor.Dispatchers;

namespace Ox.Huawei.Sms.Monitor.Tests.Doubles
{
	internal class MockDispatcher : IDispatcher
	{
		public class SmsEventArgs(SmsListMessage message) : EventArgs
		{
			public SmsListMessage Message => message;
		}
		public event EventHandler<SmsEventArgs>? SmsDispatched;

		public Task DispatchSms(SmsListMessage sms, CancellationToken cancellationToken)
		{
			SmsDispatched?.Invoke(this, new SmsEventArgs(sms));
			return Task.CompletedTask;
		}

		public event EventHandler<ErrorEventArgs>? ErrorDispatched;
		public Task DispatchError(Exception ex, CancellationToken cancellationToken)
		{
			this.ErrorDispatched?.Invoke(this, new ErrorEventArgs(ex));
			return Task.CompletedTask;
		}
	}
}
