using Ox.Huawei.Sms.Monitor.Model.Messages;
using System.Buffers;
using System.Text.Json;

namespace Ox.Huawei.Sms.Monitor
{
	/// <summary>
	/// Pipeline for control messages.  Disassembles into a concrete class.
	/// </summary>
	public sealed class ControlMessageDisassembler(ILogger<ControlMessageDisassembler>? logger = null)
	{
		private readonly static Task<MessageModel?> _nullTaskObject = Task.FromResult<MessageModel?>(null);
		private readonly ILogger<ControlMessageDisassembler>? log = logger;

		/// <summary>
		/// Processes a JSON payload and returns a concrete MessageModel, or null.
		/// </summary>
		/// <param name="payload">JSON sequence</param>
		/// <returns><see cref="MessageModel"/> implementation, or null if unrecognised.</returns>
		public Task<MessageModel?> DisassemblePayload(ReadOnlySequence<byte> payload)
		{
			Utf8JsonReader reader = new(payload, new JsonReaderOptions()
			{
				MaxDepth = 5,
				AllowTrailingCommas = true,
				CommentHandling = JsonCommentHandling.Skip,
			});

			this.log?.LogTrace("Starting read of JSON.");

			// Message open
			reader.Read();
			if (reader.TokenType != JsonTokenType.StartObject)
				return _nullTaskObject;

			this.log?.LogTrace("Message is object, looking for MessageType.");

			// Move to Message Type
			reader.Read();
			if (reader.GetString() != "MessageType")
				return _nullTaskObject;

			// Get Message Type
			reader.Read();
			string? messageType = reader.GetString();
			if (messageType == null)
				return _nullTaskObject;

			if(this.log?.IsEnabled(LogLevel.Trace) ?? false)
				this.log?.LogTrace("Message type is {MessageType}, looking for Timestamp.", messageType);

			_ = DateTimeOffset.TryParse(reader.ReadString("Timestamp"), System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat, out DateTimeOffset timestamp);

			if (this.log?.IsEnabled(LogLevel.Trace) ?? false)
				this.log?.LogTrace("Timestamp is {Timestamp}, continuing to read message.", timestamp);

			// Continue disassembly for the remainder of the message, based on type

			MessageModel? model = null;

			switch (messageType)
			{
				case "SendSms":
					if (this.log?.IsEnabled(LogLevel.Trace) ?? false)
						this.log?.LogTrace("Deserialising SendSms message.");
					DeserialiseSendSms(ref reader, ref model);
					break;
			}

			if (model != null)
			{
				model.MessageType = messageType;
				model.Timestamp = timestamp;
			}

			if (this.log?.IsEnabled(LogLevel.Trace) ?? false)
				this.log?.LogTrace("Finished reading message, returning {MessageType}.", model?.MessageType ?? "null");

			return Task.FromResult(model);

		}

		private static void DeserialiseSendSms(ref Utf8JsonReader reader, ref MessageModel? model)
		{
			var message = new SendSmsModel
			{
				To = reader.ReadStringArray("To") ?? [],
				Content = System.Text.RegularExpressions.Regex.Unescape(reader.ReadString("Content") ?? String.Empty)
			};

			model = message;
		}
	}

	file static class Extensions
	{
		public static string? ReadString(this ref Utf8JsonReader reader, string propertyName)
		{
			if (reader.ReadToProperty(propertyName))
			{
				reader.Read();
				return reader.GetString();
			}

			return null;
		}

		public static IEnumerable<string>? ReadStringArray(this ref Utf8JsonReader reader, string propertyName)
		{
			if (reader.ReadToProperty(propertyName))
			{

				reader.Read();
				List<string> vals = [];
				if (reader.TokenType == JsonTokenType.StartArray)
				{
					reader.Read();

					// This is an array, read all values
					while (reader.TokenType != JsonTokenType.EndArray)
					{
						if (reader.TokenType == JsonTokenType.String)
						{
							var val = reader.GetString();
							if (val != null)
								vals.Add(val);
						}
						reader.Read();
					}
				}
				else if (reader.TokenType == JsonTokenType.String)
				{
					// This is a single value, read it
					var val = reader.GetString();
					if (val != null)
						vals.Add(val);
				}
				return vals;
			}
			return null;
		}

		public static bool ReadToProperty(this ref Utf8JsonReader reader, string propertyName)
		{
			do
			{
				if (reader.TokenType == JsonTokenType.PropertyName
					&& reader.GetString() == propertyName)
					return true;
			} while (reader.Read());

			return false;
		}
	}
}
