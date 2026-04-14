namespace Ox.Huawei.Sms.Monitor.Config
{
	/// <summary>
	/// Configuration options for <see cref="Dispatchers.MqttDispatcher"/>
	/// </summary>
	public sealed class MqttConnectionOptions
	{
		/// <summary>
		/// Hostname of IP address of MQTT broker.
		/// </summary>
		public string? Host { get; set; }
		/// <summary>
		/// Optional. TCP port number for MQTT broker.
		/// </summary>
		public int? Port { get; set; }
		/// <summary>
		/// Optional. Username to authenticate with MQTT broker.
		/// </summary>
		public string? Username { get; set; }
		/// <summary>
		/// Optional. Password to authenticate with MQTT broker, requires <see cref="Username"/>.
		/// </summary>
		public string? Password { get; set; }
		/// <summary>
		/// Use TLS when connecting to broker.  Default false.
		/// </summary>
		public bool UseTls { get; set; } = false;
		/// <summary>
		/// Base MQTT Topic name, defaults to "SmsMonitor/received".
		/// </summary>
		public string TopicName { get; set; } = "SmsMonitor/received";
		/// <summary>
		/// Gets or sets the delay, in seconds, before attempting to reconnect after a disconnection.
		/// </summary>
		public int ReconnectDelaySeconds { get; set; } = 5; // Default delay before reconnecting
		/// <summary>
		/// Gets or sets the MQTT protocol version to use for communication.
		/// </summary>
		/// <remarks>The default value is 3, corresponding to MQTT version 3.0. Set this property to specify a
		/// different protocol version if required by the broker or client implementation.</remarks>
		public int MqttVersion { get; set; } = 3; // Default MQTT version
		/// <summary>
		/// Gets or sets the timeout duration, in seconds, for the operation.  Default 30 seconds.
		/// </summary>
		public int TimeoutSeconds { get; set; } = 30;
	}
}
