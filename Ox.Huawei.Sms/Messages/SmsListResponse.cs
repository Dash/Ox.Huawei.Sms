using System;
using System.Globalization;
using System.Xml.Serialization;

namespace Ox.Huawei.Sms.Messages
{
	[XmlRoot("response")]
	public class SmsListResponse
	{
		//response><Count>6</Count><Messages><Message><Smstat>1</Smstat><Index>40010</Index><Phone>+447788120830</Phone><Content>Ok</Content><Date>2024-08-04 17:00:38</Date><Sca></Sca><SaveType>0</SaveType><Priority>0</Priority><SmsType>1</SmsType></Message>

		[XmlElement]
		public int Count { get; set; }
		[XmlArray()]
		[XmlArrayItem("Message")]
		public SmsListMessage[]? Messages { get; set; }
	}

	public class SmsListMessage
	{
		/// <summary>
		/// 0 for unread 1 for read
		/// </summary>
		public int Smstat { get; set; }
		public int Index { get; set; }
		public string? Phone { get; set; }
		public string? Content { get; set; }
		public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
		public string? Sca { get; set; }
		public int SaveType { get; set; }
		public int Priority { get; set; }
		public int SmsType { get; set; }

		public static implicit operator Model.MessageDetail(SmsListMessage sms)
		{
			return new Model.MessageDetail(
				sms.Phone ?? String.Empty,
				sms.Content,
				DateTime.ParseExact(sms.Date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
				sms.Smstat > 0);
		}

		public override string ToString() => this.ToXmlString();
	}
}
