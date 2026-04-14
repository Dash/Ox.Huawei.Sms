using System.Net.Http;
using System.Xml.Serialization;

namespace Ox.Huawei.Sms.Messages
{
	/// <summary>
	/// Represents a request for a list of SMS messages.
	/// </summary>
	[XmlRoot(ElementName = "request")]
	public sealed class SmsList
	{
		/// <summary>
		/// Gets or sets the page index for the request.
		/// </summary>
		public int PageIndex { get; set; } = 1;
		/// <summary>
		/// Gets or sets the number of messages to read per page.
		/// </summary>
		public int ReadCount { get; set; } = 20;
		/// <summary>
		/// Gets or sets the box type (e.g., inbox, outbox).
		/// </summary>
		public int BoxType { get; set; } = 1;
		/// <summary>
		/// Gets or sets the sort type for the messages.
		/// </summary>
		public int SortType { get; set; } = 0;
		/// <summary>
		/// Gets or sets the sort order (0 for descending, 1 for ascending).
		/// </summary>
		public int Ascending { get; set; } = 0;
		/// <summary>
		/// Gets or sets a value indicating whether unread messages are preferred.
		/// </summary>
		public int UnreadPreferred { get; set; } = 0;

		private const string CACHED_CONTENT = "<?xml version=\"1.0\" encoding=\"utf-8\"?><request><PageIndex>1</PageIndex><ReadCount>20</ReadCount><BoxType>1</BoxType><SortType>0</SortType><Ascending>0</Ascending><UnreadPreferred>0</UnreadPreferred></request>";

		/// <summary>
		/// Hard-coded general request message to avoid serialisation costs.
		/// </summary>
		/// <returns>New request message (must be disposed of).</returns>
		public static HttpRequestMessage StaticRequest()
		{

			var r = new HttpRequestMessage(HttpMethod.Post, "sms/sms-list")
			{
				Content = new StringContent(CACHED_CONTENT),
			};

			r.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml") { CharSet = "utf8" };

			return r;
		}
	}
}
