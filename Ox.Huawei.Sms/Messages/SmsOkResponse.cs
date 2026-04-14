using System.Xml.Serialization;

namespace Ox.Huawei.Sms.Messages
{
	/// <summary>
	/// Represents the response to an SMS operation, containing a string content.
	/// </summary>
	[XmlRoot("response")]
	public sealed class SmsOkResponse
	{
		/// <summary>
		/// Gets or sets the response content.
		/// </summary>
		[XmlText]
		public string? Content { get; set; }

		/// <summary>
		/// Returns the response content as a string.
		/// </summary>
		public override string? ToString() => this.Content;
	}
}
