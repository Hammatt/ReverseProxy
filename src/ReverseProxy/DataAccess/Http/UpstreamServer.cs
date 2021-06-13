using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ReverseProxy.DataAccess.Http
{
    public class UpstreamServer : IUpstreamServer
    {
        private readonly HttpClient _httpClient;

        public UpstreamServer(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
        {
            return await _httpClient.SendAsync(httpRequestMessage, cancellationToken);
        }
    }
}
