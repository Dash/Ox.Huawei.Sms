namespace Ox.Huawei.Sms.Monitor.Model.Messages
{
	/// <summary>
	/// Represents a request to send an SMS message.
	/// </summary>
	public sealed class SendSmsModel : MessageModel
	{
		/// <summary>
		/// Gets or sets the recipients of the SMS message.
		/// </summary>
		public IEnumerable<String> To { get; set; } = [];

		/// <summary>
		/// Gets or sets the content of the SMS message.
		/// </summary>
		public String Content { get; set; } = String.Empty;

		/// <inheritdoc />
		public override string ToString() => $"{base.ToString()}: To: {String.Join(", ", this.To)} Content: {this.Content}";
	}
}
