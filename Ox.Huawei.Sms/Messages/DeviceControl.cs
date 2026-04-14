using System;
using System.Xml.Serialization;

namespace Ox.Huawei.Sms.Messages
{
	[XmlRoot("request")]
	public sealed class DeviceControl
	{
		/// <summary>
		/// Control signal
		/// </summary>
		/// <remarks>
		/// 1: restart
		/// </remarks>
		public DeviceControlValues Control { get; set; } = DeviceControlValues.Reboot;
	}

	public enum DeviceControlValues : UInt16
	{
		None = 0,
		Reboot = 1,
	}
}
