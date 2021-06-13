using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ReverseProxy.DataAccess
{
    public interface IUpstreamServer
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken);
    }
}
