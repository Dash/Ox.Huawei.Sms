using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Formatter;
using Ox.Huawei.Sms.Monitor.Model.Messages;
using Ox.Huawei.Sms.Monitor.Config;
using System.Buffers;

namespace Ox.Huawei.Sms.Monitor.Listeners
{
	public sealed class MqttMonitor(
		ControlMessageDisassembler messageDisassembler,
		ISmsClient smsClient,
		IOptionsMonitor<MqttConnectionOptions> mqttOptions,
		ILogger<MqttMonitor>? logger = null) : IHostedService, IDisposable
	{
		private readonly ILogger? log = logger;
		private IMqttClient? mqtt;
		private readonly MqttConnectionOptions options = mqttOptions?.Get(nameof(MqttMonitor)) ?? throw new ArgumentNullException(nameof(mqttOptions));
		private readonly ControlMessageDisassembler messageDisassembler = messageDisassembler ?? throw new ArgumentNullException(nameof(messageDisassembler));
		private readonly ISmsClient smsClient = smsClient ?? throw new ArgumentNullException(nameof(smsClient));

		private async Task CreateClient()
		{
			var config = this.options;
			var builder = new MqttClientOptionsBuilder()
				.WithProtocolVersion((MqttProtocolVersion)config.MqttVersion)
				.WithClientId($"{AppDomain.CurrentDomain.FriendlyName}_{Environment.ProcessId}_Monitor")
				.WithCleanStart()
				.WithTcpServer(config.Host, config.Port)
				.WithCredentials(config.Username, config.Password)
				.WithTlsOptions(new MqttClientTlsOptions()
				{
					UseTls = config.UseTls
				});
			this.mqtt = new MqttClientFactory().CreateMqttClient();
			await this.mqtt.ConnectAsync(builder.Build());
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (!(this.mqtt?.IsConnected ?? false))
			{
				await this.CreateClient();
			}

			_ = await this.mqtt.SubscribeAsync(topic: this.options.TopicName, cancellationToken: cancellationToken);

			this.mqtt!.ApplicationMessageReceivedAsync += async args => await this.messageDisassembler.DisassemblePayload(args.ApplicationMessage.Payload)
					.ContinueWith(async msgTask =>
					{
						if (msgTask.IsCompletedSuccessfully && msgTask.Result is not null)
						{
							var message = msgTask.Result;
							if(this.log?.IsEnabled(LogLevel.Information) ?? false)
								this.log?.LogInformation("Received message: {Message}", message);

							if (message is SendSmsModel sendSms)
							{
								if (this.log?.IsEnabled(LogLevel.Debug) ?? false)
									this.log?.LogDebug("Sending SMS to {Recipients}", string.Join(", ", sendSms.To));

								try
								{
									await this.smsClient.Send(sendSms.To.ToArray(), sendSms.Content);
								}
								catch (Exception ex)
								{
									this.log?.LogError(ex, "Failed to send SMS to {Recipients}", string.Join(", ", sendSms.To));
								}
							}
							else
							{
								if(this.log?.IsEnabled(LogLevel.Information) ?? false)
									this.log?.LogInformation("Unhandled message type: {MessageType}", message.MessageType);
							}
						}
						else
						{
							this.log?.LogWarning("Failed to disassemble message from topic {Topic}", args.ApplicationMessage.Topic);
						}
					}, TaskScheduler.Default);
		}

		public async Task StopAsync(CancellationToken cancellationToken) => await this.mqtt.UnsubscribeAsync(this.options.TopicName, cancellationToken);

		public void Dispose() => this.mqtt?.Dispose();
	}
}
