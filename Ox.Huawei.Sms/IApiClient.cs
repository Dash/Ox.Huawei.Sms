using System.Net.Http;
using System.Threading.Tasks;

namespace Ox.Huawei.Sms
{
	/// <summary>
	/// Abstraction for making HTTP requests to the device API and deserialising responses.
	/// </summary>
	public interface IApiClient
	{
		/// <summary>
		/// Sends a GET request to the specified URL and returns the deserialised response.
		/// </summary>
		/// <typeparam name="T">The expected response type. Must be a class with a parameterless constructor.</typeparam>
		/// <param name="url">The request URL or path to call.</param>
		/// <returns>A task that resolves to an instance of <typeparamref name="T"/> or <c>null</c> when no response could be obtained.</returns>
		Task<T?> GetAsync<T>(string url) where T : class, new();

		/// <summary>
		/// Sends a POST request to the specified API path with the provided request object and
		/// returns the deserialised response.
		/// </summary>
		/// <typeparam name="T">The expected response type. Must be a class with a parameterless constructor.</typeparam>
		/// <typeparam name="T2">The request payload type.</typeparam>
		/// <param name="apiPath">The API path to post to.</param>
		/// <param name="requestData">The request payload to send in the body.</param>
		/// <returns>A task that resolves to an instance of <typeparamref name="T"/> or <c>null</c> when no response could be obtained.</returns>
		Task<T?> PostAsync<T, T2>(string apiPath, T2 requestData)
			where T : class, new()
			where T2 : class;

		/// <summary>
		/// Sends a POST request with the given <see cref="HttpContent"/> to the specified URL and
		/// returns the deserialised response.
		/// </summary>
		/// <typeparam name="T">The expected response type. Must be a class with a parameterless constructor.</typeparam>
		/// <param name="url">The request URL or path to call.</param>
		/// <param name="content">The HTTP content to include in the request body.</param>
		/// <returns>A task that resolves to an instance of <typeparamref name="T"/> or <c>null</c> when no response could be obtained.</returns>
		Task<T?> PostAysnc<T>(string url, HttpContent content) where T : class, new();

		/// <summary>
		/// Sends an HTTP request using the specified <see cref="HttpMethod"/>, API path and optional request data,
		/// and returns the deserialised response.
		/// </summary>
		/// <typeparam name="T">The expected response type. Must be a class with a parameterless constructor.</typeparam>
		/// <typeparam name="T2">The request payload type.</typeparam>
		/// <param name="apiPath">The API path to call.</param>
		/// <param name="method">The HTTP method to use (GET, POST, etc.).</param>
		/// <param name="requestData">Optional request payload to include in the request body.</param>
		/// <returns>A task that resolves to an instance of <typeparamref name="T"/> or <c>null</c> when no response could be obtained.</returns>
		Task<T?> SendAsync<T, T2>(string apiPath, HttpMethod method, T2? requestData)
			where T : class, new()
			where T2 : class;

		/// <summary>
		/// Sends a fully-formed <see cref="HttpRequestMessage"/> and returns the deserialised response.
		/// </summary>
		/// <typeparam name="T">The expected response type. Must be a class with a parameterless constructor.</typeparam>
		/// <param name="req">The HTTP request message to send.</param>
		/// <returns>A task that resolves to an instance of <typeparamref name="T"/> or <c>null</c> when no response could be obtained.</returns>
		Task<T?> SendAsync<T>(HttpRequestMessage req) where T : class, new();
	}
}