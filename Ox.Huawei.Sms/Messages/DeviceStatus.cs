using System.Xml.Serialization;

namespace Ox.Huawei.Sms.Messages
{
#pragma warning disable IDE1006 // Naming Styles
	[XmlRoot("response")]
	public class DeviceStatus
	{
		[XmlElement(IsNullable = true)]
		public string? ConnectionStatus { get; set; }
		[XmlElement(IsNullable = true)]
		public string? WifiConnectionStatus { get; set; }
		[XmlElement(IsNullable = true)]
		public string? SignalStrength { get; set; }
		[XmlElement(IsNullable = true)]
		public int? SignalIcon { get; set; }
		[XmlElement(IsNullable = true)]
		public int? CurrentNetworkType { get; set; }
		[XmlElement(IsNullable = true)]
		public int? CurrentServiceDomain { get; set; }
		[XmlElement(IsNullable = true)]
		public int? RoamingStatus { get; set; }
		[XmlElement(IsNullable = true)]
		public int? BatteryStatus { get; set; }
		[XmlElement(IsNullable = true)]
		public int? BatteryLevel { get; set; }
		[XmlElement(IsNullable = true)]
		public int? BatteryPercent { get; set; }
		[XmlElement(IsNullable = true)]
		public int? simlockStatus { get; set; }
		[XmlElement(IsNullable = true)]
		public string? PrimaryDns { get; set; }
		[XmlElement(IsNullable = true)]
		public string? SecondaryDns { get; set; }
		[XmlElement(IsNullable = true)]
		public int? wififrequence { get; set; }
		[XmlElement(IsNullable = true)]
		public int? flymode { get; set; }
		[XmlElement(IsNullable = true)]
		public string? PrimaryIPv6Dns { get; set; }
		[XmlElement(IsNullable = true)]
		public string? SecondaryIPv6Dns { get; set; }
		[XmlElement(IsNullable = true)]
		public string? CurrentWifiUser { get; set; }
		[XmlElement(IsNullable = true)]
		public string? TotalWifiUser { get; set; }
		[XmlElement(IsNullable = true)]
		public int? currenttotalwifiuser { get; set; }
		[XmlElement(IsNullable = true)]
		public int? ServiceStatus { get; set; }
		[XmlElement(IsNullable = true)]
		public string? WifiStatus { get; set; }
		[XmlElement(IsNullable = true)]
		public string? CurrentNetworkTypeEx { get; set; }
		[XmlElement(IsNullable = true)]
		public int? maxsignal { get; set; }
		[XmlElement(IsNullable = true)]
		public int? wifiindooronly { get; set; }
		[XmlElement(IsNullable = true)]
		public string? classify { get; set; }
		[XmlElement(IsNullable = true)]
		public int? usbup { get; set; }
		[XmlElement(IsNullable = true)]
		public int? wifiswitchstatus { get; set; }
		[XmlElement(IsNullable = true)]
		public int? WifiStatusExCustom { get; set; }
		[XmlElement(IsNullable = true)]
		public int? hvdcp_online { get; set; }
		[XmlElement(IsNullable = true)]
		public int? speedLimitStatus { get; set; }
		[XmlElement(IsNullable = true)]
		public int? poorSignalStatus { get; set; }
	}
#pragma warning restore IDE1006 // Naming Styles
}
