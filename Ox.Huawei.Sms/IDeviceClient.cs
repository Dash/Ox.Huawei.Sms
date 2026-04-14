using Ox.Huawei.Sms.Messages;
using System.Threading.Tasks;

namespace Ox.Huawei.Sms
{
	/// <summary>
	/// Abstract interface describing telephony device management actions
	/// </summary>
	public interface IDeviceClient
	{
		/// <summary>
		/// Requests a restart of the device
		/// </summary>
		/// <returns>True on successful request (no guarantee it has worked)</returns>
		Task<bool> DeviceRestart();
		/// <summary>
		/// Retrieves information about the device's current signal
		/// </summary>
		/// <returns>Signal overview</returns>
		Task<DeviceSignal> DeviceSignal();
		/// <summary>
		/// Retieves information about the device's status
		/// </summary>
		/// <returns>Device status</returns>
		Task<DeviceStatus> DeviceStatus();
	}
}