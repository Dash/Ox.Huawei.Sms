using Ox.Huawei.Sms.Messages;
using System;
using System.Threading.Tasks;

namespace Ox.Huawei.Sms
{
	/// <summary>
	/// Provides device control mechanisms
	/// </summary>
	/// <param name="client">Device API interface</param>
	/// <inheritdoc />
	public class DeviceClient(IApiClient client) : IDeviceClient
	{
		private readonly IApiClient client = client ?? throw new ArgumentNullException(nameof(client));

		public async virtual Task<DeviceSignal> DeviceSignal() => (await this.client.GetAsync<DeviceSignal>("device/signal")) ?? new DeviceSignal();

		public async virtual Task<DeviceStatus> DeviceStatus() => (await this.client.GetAsync<DeviceStatus>("monitoring/status")) ?? new DeviceStatus();

		public async virtual Task<bool> DeviceRestart()
		{
			try
			{
				_ = await this.client.PostAsync<SmsOkResponse, DeviceControl>("device/control", new DeviceControl());
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
