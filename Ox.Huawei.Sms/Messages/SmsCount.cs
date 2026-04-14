namespace Ox.Huawei.Sms.Messages
{
	/// <summary>
	/// Represents a count of SMS messages in various folders and SIM locations.
	/// </summary>
	public sealed class SmsCount
	{
		/// <summary>Gets or sets the number of unread messages in local storage.</summary>
		public int LocalUnread { get; set; }
		/// <summary>Gets or sets the number of inbox messages in local storage.</summary>
		public int LocalInbox { get; set; }
		/// <summary>Gets or sets the number of outbox messages in local storage.</summary>
		public int LocalOutbox { get; set; }
		/// <summary>Gets or sets the number of draft messages in local storage.</summary>
		public int LocalDraft { get; set; }
		/// <summary>Gets or sets the number of deleted messages in local storage.</summary>
		public int LocalDeleted { get; set; }
		/// <summary>Gets or sets the number of unread messages on the SIM.</summary>
		public int SimUnread { get; set; }
		/// <summary>Gets or sets the number of inbox messages on the SIM.</summary>
		public int SimInbox { get; set; }
		/// <summary>Gets or sets the number of outbox messages on the SIM.</summary>
		public int SimOutbox { get; set; }
		/// <summary>Gets or sets the number of draft messages on the SIM.</summary>
		public int SimDraft { get; set; }
		/// <summary>Gets or sets the maximum number of messages in local storage.</summary>
		public int LocalMax { get; set; }
		/// <summary>Gets or sets the maximum number of messages on the SIM.</summary>
		public int SimMax { get; set; }
		/// <summary>Gets or sets the number of used SIM slots.</summary>
		public int SimUsed { get; set; }
		/// <summary>Gets or sets the number of new messages.</summary>
		public int NewMsg { get; set; }
	}
}
