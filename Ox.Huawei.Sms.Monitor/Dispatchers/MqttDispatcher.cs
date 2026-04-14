using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Formatter;
using Ox.Huawei.Sms.Messages;
using Ox.Huawei.Sms.Model;
using Ox.Huawei.Sms.Monitor.Config;
using System.Text.Json;

namespace Ox.Huawei.Sms.Monitor.Dispatchers
{
	/// <summary>
	/// Dispatches SMS messages and errors to a MQTT topic.
	/// </summary>
	/// <remarks>
	/// Handles the connection to the MQTT Broker, including dropped connections and retries. Leverages the
	/// MQTTNet Library.  Connection is held for the lifetime of the process.
	/// Distinct from <see cref="Listeners.MqttMonitor"/> which manages its own connection for listening for
	/// instructions.
	/// </remarks>
	/// <inheritdoc />
	public sealed class MqttDispatcher : IDispatcher, IDisposable
	{
		private readonly ILogger? log;
		private readonly IMqttClient mqtt;
		private readonly string topicName;
		private readonly SemaphoreSlim reconnectLock = new(1);
		private readonly TimeSpan timeout;

		public MqttDispatcher(IOptionsMonitor<MqttConnectionOptions> config, ILogger<MqttDispatcher>? logger)
		{
			this.log = logger;
			this.topicName = config.CurrentValue.TopicName;
			this.timeout = TimeSpan.FromSeconds(config.CurrentValue.TimeoutSeconds);
			this.mqtt = this.BuildMqttClient(config.CurrentValue);
		}

		private IMqttClient BuildMqttClient(MqttConnectionOptions config)
		{

			var builder = new MqttClientOptionsBuilder()
				.WithProtocolVersion((MqttProtocolVersion)config.MqttVersion)
				.WithClientId($"{AppDomain.CurrentDomain.FriendlyName}_{Environment.ProcessId}_Dispatcher")
				.WithCleanStart()
				.WithTcpServer(config.Host, config.Port)
				.WithCredentials(config.Username, config.Password)
				.WithTimeout(TimeSpan.FromSeconds(config.TimeoutSeconds))
				.WithTlsOptions(new MqttClientTlsOptions()
				{
					UseTls = config.UseTls
				});

			var opts = builder.Build();

			var clt = new MqttClientFactory().CreateMqttClient();

			clt.DisconnectedAsync += async (args) =>
			{
				this.log?.LogDebug("MQTT client disconnected, waiting lock.");
				await this.reconnectLock.WaitAsync();
				try
				{
					this.log?.LogWarning(args.Exception, "MQTT Client disconnected.");
					await Task.Delay(TimeSpan.FromSeconds(config.ReconnectDelaySeconds)); // Wait before reconnecting
					await clt.ConnectAsync(opts);
				}
				catch (Exception ex)
				{
					this.log?.LogError(ex, "Error reconnecting MQTT client.");
					throw;
				}
				finally
				{
					this.reconnectLock.Release();
				}
			};

			if (this.log?.IsEnabled(LogLevel.Debug) ?? false)
				this.log?.LogDebug("Connecting MQTT client to {Host}:{Port} with TLS={UseTls}",
				config.Host, config.Port, config.UseTls);
			clt.ConnectAsync(opts);

			return clt;
		}

		static JsonWriterOptions _writerOptions = new()
		{
			Indented = true,
			SkipValidation = true,
			IndentCharacter = '\t',
		};

		public async Task DispatchError(Exception ex, CancellationToken cancellationToken)
		{
			using var ms = new MemoryStream();
			using Utf8JsonWriter jsonWriter = new(ms, _writerOptions);

			jsonWriter.WriteStartObject();
			jsonWriter.WriteString("MessageType", "SmsError");
			jsonWriter.WriteString("Timestamp", DateTime.UtcNow.ToString("s"));
			jsonWriter.WriteStartObject("Error");
			jsonWriter.WriteString("Message", ex.Message);
			jsonWriter.WriteString("StackTrace", ex.StackTrace);
			jsonWriter.WriteEndObject();
			jsonWriter.WriteEndObject();

			await jsonWriter.FlushAsync(cancellationToken);

			ms.Seek(0, SeekOrigin.Begin);

			await this.Publish(ms, this.topicName, cancellationToken);
		}

		public async Task DispatchSms(SmsListMessage sms, CancellationToken cancellationToken)
		{
			MessageDetail msg = sms;

			using var ms = new MemoryStream();

			using Utf8JsonWriter jsonWriter = new(ms, _writerOptions);

			jsonWriter.WriteStartObject();
			jsonWriter.WriteString("MessageType", "SmsReceived");
			jsonWriter.WriteString("Timestamp", DateTime.UtcNow.ToString("s"));
			jsonWriter.WriteStartObject("Sms");
			jsonWriter.WriteString("From", msg.PhoneNumber);
			jsonWriter.WriteString("Date", msg.Date);
			jsonWriter.WriteString("Content", msg.Content);
			jsonWriter.WriteEndObject();
			jsonWriter.WriteEndObject();

			await jsonWriter.FlushAsync(cancellationToken);

			ms.Seek(0, SeekOrigin.Begin);

			await this.Publish(ms, this.topicName, cancellationToken);
		}

		private async Task Publish(Stream ms, string topicName, CancellationToken cancellationToken)
		{
			if (this.log?.IsEnabled(LogLevel.Information) ?? false)
				this.log?.LogInformation("Sending message to {Topic}", topicName);

			try
			{
				var builder = new MqttApplicationMessageBuilder()
					.WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
					.WithTopic(this.topicName)
					.WithPayload(ms);

				if ((int)this.mqtt.Options.ProtocolVersion >= 5)
					builder.WithContentType("application/json");

				var mqttMessage = builder.Build();

				while (!this.mqtt.IsConnected)
				{
					this.log?.LogWarning("MQTT client is not connected, waiting for reconnect.");

					// Wait and release immediately
					await this.reconnectLock.WaitAsync(cancellationToken);
					this.reconnectLock.Release();
				}

				if (this.log?.IsEnabled(LogLevel.Trace) ?? false)
					this.log?.LogTrace("Publishing MQTT message to {TopicName}", topicName);

				var response = await this.mqtt.PublishAsync(mqttMessage, cancellationToken).WaitAsync(this.timeout, CancellationToken.None);

				if (!response.IsSuccess)
				{
					string message = $"Failure publishing to MQTT {response.ReasonCode}: {response.ReasonString}";
					if (this.log?.IsEnabled(LogLevel.Error) ?? false)
						this.log?.LogError(message);
					throw new ApplicationException(message);
				}
			}
			catch (Exception ex)
			{
				this.log?.LogError(ex, "Error sending to MQTT.");
				throw; // Re-throw to allow the caller to handle it
			}
		}

		public void Dispose() => this.mqtt.Dispose();
	}
}
