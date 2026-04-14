using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ox.Huawei.Sms
{
	/// <summary>
	/// Defines methods for SMS client operations.
	/// </summary>
	public interface ISmsClient
	{
		/// <summary>
		/// Deletes a message by its identifier.
		/// </summary>
		Task Delete(int id);

		/// <summary>
		/// Logs out the current user.
		/// </summary>
		Task Logout();

		/// <summary>
		/// Marks a message as read by its identifier.
		/// </summary>
		Task MarkRead(int id);

		/// <summary>
		/// Sends a message to a single destination.
		/// </summary>
		Task Send(string destination, string message);

		/// <summary>
		/// Sends a message to multiple destinations.
		/// </summary>
		Task Send(string[] destination, string message);

		/// <summary>
		/// Lists all messages, optionally filtered.
		/// </summary>
		Task<IList<Messages.SmsListMessage>> SmsList(Func<Messages.SmsListMessage, bool>? filter = null);
	}
}