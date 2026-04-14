using Ox.Huawei.Sms.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ox.Huawei.Sms.Monitor.Tests.Doubles
{
	internal class MockDeviceClient(DeviceSignal? signal = null, DeviceStatus? status = null) : IDeviceClient
	{
		public Func<bool>? DeviceRestartMock;
		public Task<bool> DeviceRestart() => Task.FromResult(this.DeviceRestartMock?.Invoke() ?? true);

		public Func<DeviceSignal> DeviceSignalMock = () => signal ?? new();
		public Task<DeviceSignal> DeviceSignal() =>
			Task.FromResult(this.DeviceSignalMock.Invoke());

		public Func<DeviceStatus> DeviceStatusMock = () => status ?? new();
		public Task<DeviceStatus> DeviceStatus() => Task.FromResult(this.DeviceStatusMock.Invoke());
	}
}
