using System;
using System.Xml.Serialization;

namespace Ox.Huawei.Sms.Messages
{
	[XmlRoot("request")]
	public sealed class SmsSend
	{
		//<Index>-1</Index><Phones><Phone>{}</Phone></Phones><Sca></Sca><Content>{}</Content><Length>{}</Length><Reserved>1</Reserved><Date>{}</Date>

		public int Index { get; set; } = -1;

		/// <summary>
		/// Array of who to send to
		/// </summary>
		[XmlArray("Phones")]
		[XmlArrayItem("Phone")]
		public string[] Phone { get; set; } = [];

		// This seems to cause problems, but works ok when omitted
		//public string Sca { get; set; } = "0";

		/// <summary>
		/// Message body
		/// </summary>
		public string Content { get; set; } = String.Empty;
		/// <summary>
		/// -1 seems to work :o
		/// </summary>
		public int Length { get; set; } = -1;
		/// <summary>
		/// No idea
		/// </summary>
		public int Reserved { get; set; } = 1;
		/// <summary>
		/// -1 seems to work
		/// </summary>
		public string Date { get; set; } = "-1";
	}
}
