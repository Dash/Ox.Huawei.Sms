using System.Xml.Serialization;

namespace Ox.Huawei.Sms.Messages
{
	[XmlRoot("error")]
	public sealed class Error
	{
		//<error><code>125003</code><message /></error>
		[XmlElement("code")]
		public int Code { get; set; }
		[XmlElement("message")]
		public string? Message { get; set; }
	}
}
