using Microsoft.Extensions.Options;
using Ox.Huawei.Sms.Messages;
using System.Net.Mail;

namespace Ox.Huawei.Sms.Monitor.Dispatchers
{
	public sealed class SmtpDispatcher : IDispatcher
	{
		private SmtpClient? client;
		private readonly Lock clientLock = new();
		private readonly string toAddress;
		private readonly string fromAddress;
		private readonly string smtpHost;
		private readonly int smtpPort;
		private int waitCount = 0;
		private readonly ILogger<SmtpDispatcher>? log;

		/// <summary>
		/// Funnels send requests to a maximum of 1 at a time, maximum support by a SmtpClient.
		/// </summary>
		private readonly SemaphoreSlim semaphore = new(1);

		public SmtpDispatcher(IOptionsMonitor<SmtpDispatcherOptions> options, ILogger<SmtpDispatcher>? logger)
		{
			ArgumentNullException.ThrowIfNull(options);

			this.toAddress = options.CurrentValue.ToAddress ?? throw new ArgumentNullException(nameof(options.CurrentValue.ToAddress), "SMTP dispather requires a destination email");
			this.fromAddress = options.CurrentValue.FromAddress;
			this.smtpHost = options.CurrentValue.Host;
			this.smtpPort = options.CurrentValue.Port;
			this.log = logger;

			this.client = new SmtpClient()
			{
				Host = options.CurrentValue.Host,
				Port = options.CurrentValue.Port,
			};
		}

		private SmtpClient GetClient()
		{
			lock (this.clientLock)
			{
				if (this.client == null)
				{
					this.client = new SmtpClient()
					{
						Host = this.smtpHost,
						Port = this.smtpPort,
					};
					this.client.SendCompleted += (sender, e) =>
					{
						if (this.waitCount == 0)
						{
							// No more messages, dispose of the client until we need a new one.
							// This will close the connection to the server (which will eventually force a closure) and free-up memory until needed.
							this.log?.LogDebug("Mail queue empty, disconnecting SMTP client");
							this.client.Dispose();
							this.client = null;
						}
					};
				}
			}
			return this.client;
		}

		public async Task DispatchSms(SmsListMessage sms, CancellationToken cancellationToken)
		{
			using MailMessage message = new(new MailAddress(this.fromAddress, sms.Phone), new MailAddress(this.toAddress, null)) { Subject = $"SMS Received", Body = sms.Content };

			await this.EmailAsync(message, cancellationToken);
		}

		public async Task DispatchError(Exception ex, CancellationToken cancellationToken)
		{
			using MailMessage message = new(new MailAddress(this.fromAddress, "SMS Monitor"), new MailAddress(this.toAddress, null))
			{
				Subject = "SMS monitor failure",
				Body = ex.Message
			};
			await this.EmailAsync(message, cancellationToken);
		}

		private async Task EmailAsync(MailMessage message, CancellationToken cancellationToken)
		{
			if(this.log?.IsEnabled(LogLevel.Information) ?? false)
				this.log?.LogInformation("Sending email to {ToAddress}", this.toAddress);

			Interlocked.Increment(ref this.waitCount);
			await this.semaphore.WaitAsync(cancellationToken);
			try
			{
				await this.GetClient().SendMailAsync(message, cancellationToken);
			}
			catch (Exception ex)
			{
				this.log?.LogError(ex, "Failed to send email to {ToAddress}", this.toAddress);
				throw; // Re-throw to allow the caller to handle it
			}
			finally
			{
				this.semaphore.Release();
				Interlocked.Decrement(ref this.waitCount);
			}
		}
	}

	public sealed class SmtpDispatcherOptions
	{
		public string Host { get; set; } = "localhost";
		public int Port { get; set; } = 25;
		public string? ToAddress { get; set; } = "root@localhost";
		public string FromAddress { get; set; } = "sms@localhost";
	}
}
