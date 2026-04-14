using System;

namespace Ox.Huawei.Sms.Model
{
	/// <summary>
	/// Represents a text/sms message
	/// </summary>
	/// <param name="PhoneNumber">Telephone number sent from</param>
	/// <param name="Content">Text message itself</param>
	/// <param name="Date">Date on the message</param>
	public record MessageDetail(
		string PhoneNumber,
		string? Content,
		DateTime Date,
		bool Read = false
		)
	{
		public override string ToString() => $"{this.Date:s} [{(this.Read ? "-" : "o")}] {this.PhoneNumber} {this.Content}";
	}
}
