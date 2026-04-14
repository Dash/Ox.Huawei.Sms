using Ox.Huawei.Sms.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ox.Huawei.Sms
{
	/// <summary>
	/// Client for SMS actvities
	/// </summary>
	public class SmsClient(IApiClient client) : ISmsClient
	{
		private readonly IApiClient client = client ?? throw new ArgumentNullException(nameof(client));

		/// <summary>
		/// Lists all messages
		/// </summary>
		/// <param name="filter">Filter on results (client side)</param>
		/// <returns>List of messages from device that match conditions</returns>
		public async virtual Task<IList<SmsListMessage>> SmsList(Func<SmsListMessage, bool>? filter = null)
		{
			// Use a static request, as the message is the same each time
			using var request = Messages.SmsList.StaticRequest();
			var list = await this.client.SendAsync<SmsListResponse>(request);

			if (list == null)
				return Array.Empty<SmsListMessage>();

			IList<Messages.SmsListMessage>? messages = list.Messages;

			if (filter != null)
				messages = list.Messages?.Where(filter).ToArray();

			return messages ?? Array.Empty<SmsListMessage>();
		}

		/// <summary>
		/// Marks a message as read, but leaves on device
		/// </summary>
		/// <param name="id">Identifier for message</param>
		public async virtual Task MarkRead(int id) =>
			// Not enough experience to know whether we might get other responses than "OK"
			_ = await this.client.SendAsync<SmsOkResponse, SmsIndex>("sms/set-read", HttpMethod.Post, new SmsIndex(id));

		/// <summary>
		/// Deletes a message from the device
		/// </summary>
		/// <param name="id">Identifier for message</param>
		public async virtual Task Delete(int id) => _ = await this.client.SendAsync<SmsOkResponse, SmsIndex>("sms/delete-sms", HttpMethod.Post, new SmsIndex(id));

		/// <summary>
		/// Logs out, doesn't seem to do anything on mine
		/// </summary>
		/// <returns></returns>
		public async virtual Task Logout() => _ = await this.client.SendAsync<SmsOkResponse, UserLogout>("user/logout", HttpMethod.Post, new UserLogout());

		/// <summary>
		/// Sends a message
		/// </summary>
		/// <param name="destination">Number to send to (no spaces)</param>
		/// <param name="message">Messsage to send (160 chars per message)</param>
		public virtual Task Send(string destination, string message) => this.Send([destination], message);

		/// <summary>
		/// Sends a message to multiple recipients
		/// </summary>
		/// <param name="destination">Numbers to send to</param>
		/// <param name="message">Message to send (160 chars per message)</param>
		public async virtual Task Send(string[] destination, string message)
		{
			for (int i = 0; i < destination.Length; i++)
			{
				// Strip spaces from destination
				destination[i] = destination[i].Replace(" ", string.Empty);
			}

			var request = new SmsSend()
			{
				Content = message,
				Phone = destination
			};

			_ = await this.client.SendAsync<SmsOkResponse, SmsSend>("sms/send-sms", HttpMethod.Post, request);

		}
	}
}
