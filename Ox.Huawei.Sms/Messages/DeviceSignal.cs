using System.Xml.Serialization;

namespace Ox.Huawei.Sms.Messages
{
#pragma warning disable IDE1006 // Naming Styles
	// TODO: parse the results into stronger types
	/// <summary>
	/// Device signal information, no idea what 95% of this is
	/// </summary>
	[XmlRoot("response")]
	public sealed class DeviceSignal
	{
		public string? pci { get; set; }
		public string? sc { get; set; }
		public string? cell_id { get; set; }
		public string? rsrq { get; set; }
		public string? rsrp { get; set; }
		public string? rssi { get; set; }
		public string? sinr { get; set; }
		public string? rscp { get; set; }
		public string? ecio { get; set; }
		public string? mode { get; set; }
		public string? ulbandwidth { get; set; }
		public string? dlbandwidth { get; set; }
		public string? txpower { get; set; }
		public string? tdd { get; set; }
		public string? ul_mcs { get; set; }
		public string? dl_mcs { get; set; }
		public string? earfcn { get; set; }
		public string? rcc_status { get; set; }
		public string? rac { get; set; }
		public string? lac { get; set; }
		public string? tac { get; set; }
		public string? band { get; set; }
		public string? nei_callid { get; set; }
		public string? plmn { get; set; }
		public string? ims { get; set; }
		public string? wdlfreq { get; set; }
		public string? lteulfreq { get; set; }
		public string? ltedlfreq { get; set; }
		public string? transmode { get; set; }
		public string? enodeb_id { get; set; }
		public string? cqi0 { get; set; }
		public string? cqi1 { get; set; }
		public string? ulfrequency { get; set; }
		public string? dlfrequency { get; set; }
		public string? arfcn { get; set; }
		public string? bsic { get; set; }
		public string? rxlev { get; set; }
	}
#pragma warning restore IDE1006 // Naming Styles
}
