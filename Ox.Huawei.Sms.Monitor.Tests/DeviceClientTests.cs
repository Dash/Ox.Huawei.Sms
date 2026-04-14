using Ox.Huawei.Sms.Messages;

namespace Ox.Huawei.Sms.Monitor.Tests
{
	[TestClass]
	public class DeviceClientTests
	{
		private sealed class FakeApiClient : IApiClient
		{
			public Func<string, object?>? GetAsyncResult;
			public Func<string, object?, object?>? PostAsyncResult;
			public Func<string, HttpContent, object?>? PostAysncResult;
			public Func<string, HttpMethod, object?, object?>? SendAsyncResult;
			public Func<HttpRequestMessage, object?>? SendAsyncReqResult;

			public Task<T?> GetAsync<T>(string url) where T : class, new()
			{
				if (this.GetAsyncResult is null)
					return Task.FromResult<T?>(null);
				var res = this.GetAsyncResult(url);
				return Task.FromResult(res as T);
			}

			public Task<T?> PostAsync<T, T2>(string apiPath, T2 requestData) where T : class, new() where T2 : class
			{
				if (this.PostAsyncResult is null)
					return Task.FromResult<T?>(null);
				var res = this.PostAsyncResult(apiPath, requestData);
				return Task.FromResult(res as T);
			}

			public Task<T?> PostAysnc<T>(string url, HttpContent content) where T : class, new()
			{
				if (this.PostAysncResult is null)
					return Task.FromResult<T?>(null);
				var res = this.PostAysncResult(url, content);
				return Task.FromResult(res as T);
			}

			public Task<T?> SendAsync<T, T2>(string apiPath, HttpMethod method, T2? requestData) where T : class, new() where T2 : class
			{
				if (this.SendAsyncResult is null)
					return Task.FromResult<T?>(null);
				var res = this.SendAsyncResult(apiPath, method, requestData);
				return Task.FromResult(res as T);
			}

			public Task<T?> SendAsync<T>(HttpRequestMessage req) where T : class, new()
			{
				if (this.SendAsyncReqResult is null)
					return Task.FromResult<T?>(null);
				var res = this.SendAsyncReqResult(req);
				return Task.FromResult(res as T);
			}
		}

		[TestMethod]
		public async Task DeviceSignal_ReturnsFromApi_WhenPresent()
		{
			var fake = new FakeApiClient
			{
				GetAsyncResult = (url) => new DeviceSignal { pci = "123" }
			};
			var client = new DeviceClient(fake);
			var sig = await client.DeviceSignal();
			Assert.IsNotNull(sig);
			Assert.AreEqual("123", sig.pci);
		}

		[TestMethod]
		public async Task DeviceSignal_ReturnsDefault_WhenApiReturnsNull()
		{
			var fake = new FakeApiClient();
			// no setup -> returns null
			var client = new DeviceClient(fake);
			var sig = await client.DeviceSignal();
			Assert.IsNotNull(sig);
			Assert.IsNull(sig.pci);
		}

		[TestMethod]
		public async Task DeviceStatus_ReturnsFromApi_WhenPresent()
		{
			var fake = new FakeApiClient
			{
				GetAsyncResult = (url) => new DeviceStatus { ConnectionStatus = "ok" }
			};
			var client = new DeviceClient(fake);
			var st = await client.DeviceStatus();
			Assert.IsNotNull(st);
			Assert.AreEqual("ok", st.ConnectionStatus);
		}

		[TestMethod]
		public async Task DeviceStatus_ReturnsDefault_WhenApiReturnsNull()
		{
			var fake = new FakeApiClient();
			var client = new DeviceClient(fake);
			var st = await client.DeviceStatus();
			Assert.IsNotNull(st);
			Assert.IsNull(st.ConnectionStatus);
		}

		[TestMethod]
		public async Task DeviceRestart_ReturnsTrue_OnSuccess()
		{
			var fake = new FakeApiClient
			{
				PostAsyncResult = (path, data) => new SmsOkResponse { Content = "OK" }
			};
			var client = new DeviceClient(fake);
			var res = await client.DeviceRestart();
			Assert.IsTrue(res);
		}

		[TestMethod]
		public async Task DeviceRestart_ReturnsFalse_OnException()
		{
			var fake = new FakeApiClient
			{
				PostAsyncResult = (path, data) => throw new InvalidOperationException()
			};
			var client = new DeviceClient(fake);
			var res = await client.DeviceRestart();
			Assert.IsFalse(res);
		}
	}
}
