using Ox.Huawei.Sms.Messages;

namespace Ox.Huawei.Sms.Monitor.Tests.Doubles
{
	internal class MockSmsClient : ISmsClient
	{
		public Task Delete(int id) => Task.CompletedTask;

		public Task Logout() => Task.CompletedTask;

		public Task MarkRead(int id) => Task.CompletedTask;

		public Task Send(string destination, string message) => Task.CompletedTask;

		public Task Send(string[] destination, string message) => Task.CompletedTask;

		public Func<Func<SmsListMessage, bool>?, IList<SmsListMessage>>? SmsListMock;

		public Task<IList<SmsListMessage>> SmsList(Func<SmsListMessage, bool>? filter = null)
		{
			IList<SmsListMessage> list = [];
			return Task.FromResult(this.SmsListMock?.Invoke(filter) ?? list);
		}
	}
}
