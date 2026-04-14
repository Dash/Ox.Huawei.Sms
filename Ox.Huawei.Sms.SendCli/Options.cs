namespace Ox.Huawei.Sms.SendCli
{
	/// <summary>
	/// Represents command-line options for the SMS sending CLI.
	/// </summary>
	internal struct Options
	{
		/// <summary>
		/// Gets or sets the base address for the API.
		/// </summary>
		public string BaseAddress;
		/// <summary>
		/// Gets or sets the recipients to send to.
		/// </summary>
		public string[] to;
		/// <summary>
		/// Gets or sets a value indicating whether the options are valid.
		/// </summary>
		public bool valid;
	}
}
