using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Learnify.Services
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri)
            {
                Content = content
            };
            return await client.SendAsync(request, cancellationToken);
        }
    }
}
