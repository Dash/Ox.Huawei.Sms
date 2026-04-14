using Ox.Huawei.Sms.Messages;

namespace Ox.Huawei.Sms.Monitor.Dispatchers
{
	/// <summary>
	/// Defines methods for dispatching errors and SMS messages.
	/// </summary>
	public interface IDispatcher
	{
		/// <summary>
		/// Dispatches an error asynchronously.
		/// </summary>
		/// <param name="ex">The exception to dispatch.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		Task DispatchError(Exception ex, CancellationToken cancellationToken);

		/// <summary>
		/// Dispatches an SMS message asynchronously.
		/// </summary>
		/// <param name="sms">The SMS message to dispatch.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		Task DispatchSms(SmsListMessage sms, CancellationToken cancellationToken);
	}
}
