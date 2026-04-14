namespace Ox.Huawei.Sms.Monitor.Model.Messages
{
	/// <summary>
	/// Base class for all message models.
	/// </summary>
	public class MessageModel
	{
		/// <summary>
		/// Gets or sets the message type.
		/// </summary>
		public string MessageType { get; set; } = String.Empty;

		/// <summary>
		/// Gets or sets the timestamp of the message.
		/// </summary>
		public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

		/// <summary>
		/// Returns a string representation of the message.
		/// </summary>
		public override string ToString() => $"{this.MessageType} at {this.Timestamp:O}";
	}
}
