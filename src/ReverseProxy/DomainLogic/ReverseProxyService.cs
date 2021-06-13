using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReverseProxy.DataAccess;

namespace ReverseProxy.DomainLogic
{
    public class ReverseProxyService
    {
        private readonly DestinationService _destinationService;
        private readonly ILogger _logger;

        public ReverseProxyService(DestinationService destinationService, ILogger<ReverseProxyService> logger)
        {
            _destinationService = destinationService;
            _logger = logger;
        }

        public async Task<HttpResponseMessage?> ForwardMessageAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
        {
            HttpResponseMessage? response = null;

            IUpstreamServer? upstreamServer = _destinationService.GetDestination(httpRequestMessage);

            if (upstreamServer == null)
            {
                _logger.LogInformation("Couldn't find a destination for request");
            }
            else
            {
                _logger.LogDebug("Sending to upstream server");
                response = await upstreamServer.SendAsync(httpRequestMessage, cancellationToken);
            }

            return response;
        }
    }
}
