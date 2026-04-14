using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ox.Huawei.Sms
{
	/// <summary>
	/// Manages calls to the Huawei device, and utilised by <see cref="SmsClient"/>
	/// </summary>
	/// <remarks>
	/// A single instance of this should be injected into any clients (e.g. <see cref="SmsClient"/>), which themselves
	/// contain any method-specific logic.
	/// This class handles enriching requests with security tokens and boiler-plate deserialisation
	/// </remarks>
	public class ApiClient(HttpClient httpClient) : IApiClient
	{
		private readonly HttpClient http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
		private string? sessionId = null;
		private string? sessionToken = null;
		private readonly static SemaphoreSlim _semaphore = new(1, 1);

		/// <summary>
		/// Spawns a new HttpClient with a cookie container setup
		/// </summary>
		/// <param name="baseAddress">Base address with the /api/ part of the path included, e.g. http://192.168.8.1/api</param>
		/// <returns>New HttpClient</returns>
		public static HttpClient CreateHttpClient(ref readonly string baseAddress)
		{
			var handler = new HttpClientHandler() { CookieContainer = new() };
			return new HttpClient(handler) { BaseAddress = new Uri(baseAddress), Timeout = new TimeSpan(hours: 0, minutes: 0, seconds: 5) };
		}

		/// <summary>
		/// Creates a session with HiLink
		/// </summary>
		/// <exception cref="InvalidOperationException">Session wasn't returned</exception>
		protected async virtual Task GetSession()
		{
			using var response = await this.http.GetAsync("webserver/SesTokInfo");
			var sessionRaw = await response.Content.ReadAsXmlDocAsync() ?? throw new InvalidOperationException("No valid session token found.");

			// TODO: Read without XmlDocument

			this.sessionToken = sessionRaw.SelectSingleNode("//*[local-name()='TokInfo']")?.InnerText ?? throw new InvalidOperationException("No valid session token found.");

			this.sessionId = sessionRaw.SelectSingleNode("//*[local-name()='SesInfo']")?.InnerText ?? throw new InvalidOperationException("No valid session token found.");
		}

		/// <summary>
		/// Performs GET requests with relevant tokens
		/// </summary>
		/// <typeparam name="T">Expected response object</typeparam>
		/// <param name="url">Sub-path for this API call</param>
		/// <returns>Deserialised object, or null</returns>
		public async Task<T?> GetAsync<T>(string url)
			where T : class, new()
		{
			using HttpRequestMessage req = new(HttpMethod.Get, url);

			return await this.SendAsync<T>(req);
		}

		/// <summary>
		/// Makes HTTP request to the API with relevant tokens
		/// </summary>
		/// <typeparam name="T">Type of expected return objet</typeparam>
		/// <param name="req">Request object</param>
		/// <returns>POCO response object</returns>
		/// <exception cref="ApplicationException">Error encountered submitting</exception>
		public async Task<T?> SendAsync<T>(HttpRequestMessage req)
			where T : class, new()
		{

			if (this.sessionId == null)
				await this.GetSession();


			Stream ms = null!;
			HttpResponseMessage? response = null;

			try
			{
				if (!req.Headers.TryGetValues("Cookie", out _))
					req.Headers.Add("Cookie", "SessionID=" + this.sessionId);

				// Lock to a single thread as the verification token is stateful and sequential, we have to wait for the response before the next
				await _semaphore.WaitAsync();

				bool released = false;

				try
				{
					// Set stateful token
					req.Headers.Add("__RequestVerificationToken", this.sessionToken);

					// Make request
					response = await this.http.SendAsync(req);

					// Update session token
					/* Behaviour is unknown here, does a missing or empty token mean the session is invalidated?
					 * Playing it safe by assuming any existing session is valid until explicitly blanked.
					 */
					if (response.Headers.TryGetValues("__RequestVerificationToken", out IEnumerable<string>? newToken) && newToken.Any())
					{
						this.sessionToken = newToken.First();
					}

					// Release asap
					_semaphore.Release();
					released = true;

					// Read whole stream into memory as we might need a couple of attempts to decode it if there is an error
					ms = await response.Content.ReadAsStreamAsync();
				}
				finally
				{
					// If we didn't release before, release now.
					if (!released)
						_semaphore.Release();
				}

				try
				{
					// Try and read what we're expecting
					var data = await ms.FromXmlStreamAsync<T>() ?? throw new InvalidOperationException("Response was not of expected type, and could not be deserialised.");
					return data;
				}
				catch (InvalidOperationException ex)
				{
					// Assume this is an error, try and read that, let it rethrow if it fails
					var error = await ms.FromXmlStreamAsync<Messages.Error>();
					if (error != null)
						throw new ApplicationException($"{error.Code}: {error.Message}");

					throw new ApplicationException("Unexpected response.", ex);
				}

			}
			finally
			{
				if(ms != null)
					await ms.DisposeAsync();

				response?.Dispose();
			}
		}

		/// <summary>
		/// Makes HTTP request to the API with relevant tokens
		/// </summary>
		/// <typeparam name="T">Expected response type</typeparam>
		/// <typeparam name="T2">Request type</typeparam>
		/// <param name="apiPath">Sub-path for method</param>
		/// <param name="method">HTTP method</param>
		/// <param name="requestData">Xml Data to POST</param>
		/// <returns></returns>
		public async Task<T?> SendAsync<T, T2>(string apiPath, HttpMethod method, T2? requestData)
			where T : class, new()
			where T2 : class
		{
			using HttpRequestMessage request = new(method, apiPath);

			// GETs etc don't require a body
			// Split into two calls to avoid allocating objects when not needed.
			if (requestData != null)
			{
				var ms = new MemoryStream();
				await requestData.ToXmlStream(ms);
				using var content = new StreamContent(ms);	// Will dispose of the stream for us
				request.Content = content;
				request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };

				return await this.SendAsync<T>(request);
			}
			else
			{
				return await this.SendAsync<T>(request);
			}
		}

		/// <summary>
		/// Performs POST requests with the relevant tokens
		/// </summary>
		/// <typeparam name="T">Expected response type</typeparam>
		/// <typeparam name="T2">Request type</typeparam>
		/// <param name="apiPath">Sub-path for method</param>
		/// <param name="requestData">Xml Data to POST</param>
		/// <returns></returns>
		public async Task<T?> PostAsync<T, T2>(string apiPath, T2 requestData)
			where T : class, new()
			where T2 : class => await this.SendAsync<T, T2>(apiPath, HttpMethod.Post, requestData);

		/// <summary>
		/// Performs POST requests with relevant tokens
		/// </summary>
		/// <typeparam name="T">Expected response object</typeparam>
		/// <param name="url">Sub-ath for this API call</param>
		/// <param name="content">Request object</param>
		/// <returns>POCO response object</returns>
		public async Task<T?> PostAysnc<T>(string url, HttpContent content)
			where T : class, new()
		{
			using HttpRequestMessage req = new(HttpMethod.Post, url)
			{
				Content = content
			};

			return await this.SendAsync<T>(req);
		}
	}
}
