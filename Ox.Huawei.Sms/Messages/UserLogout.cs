using System.Xml.Serialization;

namespace Ox.Huawei.Sms.Messages
{
	/// <summary>
	/// Represents a request to log out a user.
	/// </summary>
	[XmlRoot("request")]
	public class UserLogout
	{
		/// <summary>
		/// Gets or sets the logout flag.
		/// </summary>
		public int Logout { get; set; } = 1;
	}
}
