using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Ox.Huawei.Sms.Monitor.Dispatchers;
using Ox.Huawei.Sms.Monitor.Listeners;
using Ox.Huawei.Sms.Monitor.Config;

namespace Ox.Huawei.Sms.Monitor
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			IHost host;

			Console.WriteLine($"Version: {typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");

#pragma warning disable S1199 // Nested code blocks should not be used, loads of setup that can drop out of scope before Run is called.
			{
				var builder = Host.CreateApplicationBuilder(args);

				builder.AddLinuxConfigFile();

				// Set host options
				builder.Services.Configure<HostOptions>(o =>
					o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost
				);

				// Define logging
				builder.Services.AddLogging(loggingBuilder =>
				{
					loggingBuilder.ClearProviders();
					loggingBuilder.AddSimpleConsole(options =>
					{
						options.IncludeScopes = true;
						options.TimestampFormat = "[HH:mm:ss] ";
					});
				});

				// Create API client
				string dongleAddress = builder.Configuration.GetValue<string>("Device:BaseAddress", "http://192.168.8.1/api/");
				builder.Services.AddSingleton<IApiClient>((s) => new ApiClient(ApiClient.CreateHttpClient(ref dongleAddress)));

				// Register access clients
				builder.Services.AddSingleton<IDeviceClient, DeviceClient>();
				builder.Services.AddSingleton<ISmsClient, SmsClient>();

				// Get configuration options
				builder.Services.Configure<SmsMonitorOptions>(builder.Configuration.GetSection("Monitors:SmsMonitor"));
				builder.Services.Configure<DeviceMonitorOptions>(builder.Configuration.GetSection("Monitors:DeviceMonitor"));

				if (builder.Configuration.GetSection("Dispatchers:SmtpDispatcher").Exists())
				{
					builder.Services.Configure<SmtpDispatcherOptions>(builder.Configuration.GetSection("Dispatchers:SmtpDispatcher"));
					builder.Services.AddSingleton<IDispatcher, SmtpDispatcher>();
				}

				if (builder.Configuration.GetSection("Dispatchers:MqttDispatcher").Exists())
				{
					builder.Services.Configure<MqttConnectionOptions>(builder.Configuration.GetSection("Dispatchers:MqttDispatcher"));
					builder.Services.AddSingleton<IDispatcher, MqttDispatcher>();
				}

				if (builder.Configuration.GetSection("Monitors:MqttMonitor").Exists())
				{
					builder.Services.Configure<MqttConnectionOptions>(nameof(MqttMonitor), builder.Configuration.GetSection("Monitors:MqttMonitor"));
					builder.Services.AddSingleton<ControlMessageDisassembler>();
					builder.Services.AddHostedService<MqttMonitor>();
				}

				// Register Dispatcher broker to allow for multiple parallel dispatchers
				builder.Services.AddSingleton<DispatcherBroker>();

				// Register services
				builder.Services.AddHostedService<DeviceMonitor>();
				builder.Services.AddHostedService<SmsMonitor>();

				host = builder.Build();
			}
#pragma warning restore S1199 // Nested code blocks should not be used

			// Go
			host.Run();
		}
	}
}