using System.Xml.Serialization;

namespace Ox.Huawei.Sms.Messages
{
	/// <summary>
	/// Represents a request to delete or mark an SMS message by index.
	/// </summary>
	[XmlRoot("request")]
	public sealed class SmsIndex(int index)
	{
		public SmsIndex() : this(-1) { }

		/// <summary>
		/// Gets or sets the index of the SMS message.
		/// </summary>
		public int Index { get; set; } = index;
	}
}
