using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Ox.Huawei.Sms
{
	/// <summary>
	/// Common extension methods for this project
	/// </summary>
	internal static class Extensions
	{
		private static readonly XmlAttributeOverrides _overrides = GenerateOverrides();
		private static readonly XmlSerializerNamespaces _serialiserNamespaces = ConfigureSerialiserNamespace();
		private static readonly XmlWriterSettings _writerSettings = ConfigureXmlWriterSettings();

		private static XmlSerializerNamespaces ConfigureSerialiserNamespace()
		{
			var xsn = new XmlSerializerNamespaces();
			xsn.Add(String.Empty, String.Empty);
			return xsn;
		}

		private static XmlWriterSettings ConfigureXmlWriterSettings() => new()
		{
			OmitXmlDeclaration = false,
			Indent = false,
			ConformanceLevel = ConformanceLevel.Document,
			Async = true,
			NewLineHandling = NewLineHandling.None,
			NewLineOnAttributes = false,
		};

		private static XmlAttributeOverrides GenerateOverrides()
		{
			// Wip, for trying to use xml generator when there are many objects with the same name in the global
			// namespace.  I think this approach might be the answer, but isn't currently working.
			var overrides = new XmlAttributeOverrides();
			var requestAttributes = new XmlAttributes();
			var responseAttributes = new XmlAttributes();

			requestAttributes.XmlRoot = new XmlRootAttribute() { ElementName = "request" };
			responseAttributes.XmlRoot = new XmlRootAttribute() { ElementName = "response" };

			overrides.Add(typeof(Messages.DeviceControl), requestAttributes);
			overrides.Add(typeof(Messages.SmsIndex), requestAttributes);
			overrides.Add(typeof(Messages.SmsList), requestAttributes);
			overrides.Add(typeof(Messages.SmsSend), requestAttributes);
			overrides.Add(typeof(Messages.UserLogout), responseAttributes);

			overrides.Add(typeof(Messages.DeviceSignal), responseAttributes);
			overrides.Add(typeof(Messages.DeviceStatus), responseAttributes);
			overrides.Add(typeof(Messages.SmsListResponse), responseAttributes);
			overrides.Add(typeof(Messages.SmsOkResponse), responseAttributes);

			return overrides;
		}

		/// <summary>
		/// Loads a HTTP response into an <see cref="XmlDocument"/>
		/// </summary>
		/// <param name="content">HttpContent containing XML data</param>
		/// <returns>XmlDocument of the response stream</returns>
		public async static Task<XmlDocument> ReadAsXmlDocAsync(this HttpContent content)
		{
			XmlDocument xDoc = new();
			xDoc.Load(await content.ReadAsStreamAsync());
			return xDoc;
		}

		/// <summary>
		/// Reads a specific XmlNode from the http response stream
		/// </summary>
		/// <param name="content">HttpContent containing XML data</param>
		/// <param name="xpath">XPath for single node to target</param>
		/// <returns>Node or null if not found</returns>
		public async static Task<XmlNode?> ReadXmlNodeAsync(this HttpContent content, string xpath)
		{
			var doc = await content.ReadAsXmlDocAsync();
			return doc.SelectSingleNode(xpath);
		}

		/// <summary>
		/// Serialises an object into an Xml Stream
		/// </summary>
		/// <typeparam name="T">Type of object to serialise</typeparam>
		/// <param name="data">Object instance for serialisation</param>
		/// <param name="xmlStream">Output stream to write XML to</param>
		/// <param name="rewind">Rewind the stream once written</param>
		/// <exception cref="InvalidOperationException">Stream is read-only</exception>
		public static async Task ToXmlStream<T>(this T data, Stream xmlStream, bool rewind = true)
			where T : class
		{
			if (!xmlStream.CanWrite)
				throw new InvalidOperationException("Cannot write to readonly stream");

			// XmlSerializer takes care of caching, so we don't need to cache this class.
			XmlSerializer xs = new(typeof(T));

			using XmlWriter writer = XmlWriter.Create(xmlStream, _writerSettings);

			xs.Serialize(writer, data, _serialiserNamespaces);

			await writer.FlushAsync();

			if (rewind && xmlStream.CanSeek)
				xmlStream.Position = 0;
		}

		/// <summary>
		/// Serialises an object into an XML string
		/// </summary>
		/// <typeparam name="T">Type of object to serialise</typeparam>
		/// <param name="data">Object instance for serialisation</param>
		/// <returns>Xml string</returns>
		public static string ToXmlString<T>(this T data)
			where T : class
		{
			StringBuilder builder = new StringBuilder();
			using XmlWriter writer = XmlWriter.Create(builder);

			XmlSerializer xs = new(typeof(T));
			xs.Serialize(writer, data, _serialiserNamespaces);

			return builder.ToString();
		}

		/// <summary>
		/// Deserialises an XML stream into an object
		/// </summary>
		/// <typeparam name="T">Type of object to deserialise into</typeparam>
		/// <param name="stream">XML Stream</param>
		/// <returns>Deserialised instance of object</returns>
		public static Task<T?> FromXmlStreamAsync<T>(this Stream stream)
			where T : class, new()
		{
			if (stream.CanSeek) stream.Position = 0;
			XmlSerializer xs = new(typeof(T));
			return Task.FromResult(xs.Deserialize(stream) as T);
		}

		/// <summary>
		/// Deserialises an XML stream into an object
		/// </summary>
		/// <typeparam name="T">Type of object to deserialise into</typeparam>
		/// <param name="content">Response with XML Stream</param>
		/// <returns>Deserialised instance of object</returns>
		public async static Task<T?> FromXmlStreamAsync<T>(this HttpContent content)
			where T : class, new() => await (await content.ReadAsStreamAsync()).FromXmlStreamAsync<T>();

		/// <summary>
		/// Extracts the error message from a <see cref="Messages.Error"/> Http response.
		/// </summary>
		/// <param name="content">Http response with XML error</param>
		/// <returns>Error message or null if unable to retrieve</returns>
		public async static Task<(int code, string? message)?> TryGetXmlErrorAsync(this HttpContent content)
		{
			try
			{
				Messages.Error? error = await content.FromXmlStreamAsync<Messages.Error>();
				if (error != null)
				{
					return (error.Code, error.Message);
				}
			}
			catch { }

			return null;
		}
	}
}
