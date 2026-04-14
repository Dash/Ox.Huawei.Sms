using Ox.Huawei.Sms.Monitor.Model.Messages;
using System.Buffers;
using System.Text;

namespace Ox.Huawei.Sms.Monitor.Tests
{
	[TestClass()]
	public class ControlMessageDisassemblerTests
	{
		[TestMethod()]
		public async Task DisassemblePayloadTest()
		{
			var timestamp = DateTimeOffset.UtcNow;
			Guid[] guids = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];

			var payload = $@"{{
	""MessageType"": ""SendSms"",
	""Timestamp"": ""{timestamp:s}Z"",
	""To"": [ ""01234 567890"" ],
	""Content"": ""1: {guids[0]}\n2: {guids[1]}""
}}";

			Console.WriteLine(payload);

			var seq = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(payload));

			ControlMessageDisassembler disassembler = new();
			var msg = await disassembler.DisassemblePayload(seq);

			Console.WriteLine(msg.ToString());
			Assert.IsNotNull(msg);
			Assert.IsInstanceOfType<SendSmsModel>(msg);
			Assert.AreEqual("SendSms", msg.MessageType);
			Assert.AreEqual(timestamp.TotalOffsetMinutes, msg.Timestamp.TotalOffsetMinutes);

			var sendSms = (SendSmsModel)msg;
			Assert.AreEqual(1, sendSms.To.Count());
			Assert.AreEqual("01234 567890", sendSms.To.First());

			Assert.Contains(guids[0].ToString(), sendSms.Content);
			Assert.Contains(guids[1].ToString(), sendSms.Content);

		}
	}
}