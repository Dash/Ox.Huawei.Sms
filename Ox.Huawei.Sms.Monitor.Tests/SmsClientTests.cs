using Ox.Huawei.Sms.Messages;

namespace Ox.Huawei.Sms.Monitor.Tests
{
	[TestClass]
	public class SmsClientTests
	{
		private sealed class FakeApiClient : IApiClient
		{
			public Func<string, HttpMethod, object?, object?>? SendAsyncResult;
			public Func<string, object?>? GetAsyncResult;
			public Func<string, object?, object?>? PostAsyncResult;
			public Func<string, HttpContent, object?>? PostAysncResult;
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
		public async Task SmsList_ReturnsEmpty_WhenApiReturnsNull()
		{
			var fake = new FakeApiClient
			{
				SendAsyncResult = (path, method, req) => null
			};
			var client = new SmsClient(fake);
			var list = await client.SmsList();
			Assert.IsNotNull(list);
			Assert.IsEmpty(list);
		}

		[TestMethod]
		public async Task SmsList_ReturnsMessages_WhenPresent()
		{
			var fake = new FakeApiClient();
			var msgs = new SmsListMessage[] { new() { Index = 1, Content = "Hello" } };
			fake.SendAsyncResult = (path, method, req) => new SmsListResponse { Messages = msgs };
			var client = new SmsClient(fake);
			var list = await client.SmsList();
			Assert.IsNotNull(list);
			Assert.HasCount(1, list);
			Assert.AreEqual(1, list[0].Index);
			Assert.AreEqual("Hello", list[0].Content);
		}

		[TestMethod]
		public async Task SmsList_AppliesFilter()
		{
			var fake = new FakeApiClient();
			var msgs = new SmsListMessage[] {
				new() { Index = 1, Content = "One" },
				new() { Index = 2, Content = "Two" }
			};
			fake.SendAsyncResult = (path, method, req) => new SmsListResponse { Messages = msgs };
			var client = new SmsClient(fake);
			var list = await client.SmsList(m => m.Index == 2);
			Assert.HasCount(1, list);
			Assert.AreEqual(2, list[0].Index);
		}

		[TestMethod]
		public async Task MarkRead_CallsApi_WithCorrectIndex()
		{
			var fake = new FakeApiClient();
			string? calledPath = null;
			HttpMethod? calledMethod = null;
			object? calledReq = null;
			fake.SendAsyncResult = (path, method, req) => { calledPath = path; calledMethod = method; calledReq = req; return new SmsOkResponse { Content = "OK" }; };
			var client = new SmsClient(fake);
			await client.MarkRead(5);
			Assert.AreEqual("sms/set-read", calledPath);
			Assert.AreEqual(HttpMethod.Post, calledMethod);
			Assert.IsNotNull(calledReq);
			Assert.IsInstanceOfType<SmsIndex>(calledReq);
			Assert.AreEqual(5, ((SmsIndex)calledReq).Index);
		}

		[TestMethod]
		public async Task Delete_Logout_Send_CallsApi()
		{
			var fake = new FakeApiClient();
			var calls = new List<(string path, HttpMethod method, object? req)>();
			fake.SendAsyncResult = (path, method, req) => { calls.Add((path, method, req)); return new SmsOkResponse { Content = "OK" }; };
			var client = new SmsClient(fake);
			await client.Delete(7);
			await client.Logout();
			await client.Send(" 123  ", "Hi");
			// Three calls should have been made: delete, logout, send
			Assert.HasCount(3, calls);
			Assert.AreEqual("sms/delete-sms", calls[0].path);
			Assert.IsInstanceOfType<SmsIndex>(calls[0].req);
			Assert.AreEqual(7, ((SmsIndex)calls[0].req).Index);
			Assert.AreEqual("user/logout", calls[1].path);
			Assert.AreEqual("sms/send-sms", calls[2].path);
			Assert.IsInstanceOfType<SmsSend>(calls[2].req);
			var sendReq = (SmsSend)calls[2].req;
			// Ensure spaces were stripped from phone numbers
			CollectionAssert.AreEqual(new string[] { "123" }, sendReq.Phone);
			Assert.AreEqual("Hi", sendReq.Content);
		}
	}
}
