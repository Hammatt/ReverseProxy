using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using ReverseProxy.DataAccess;

namespace ReverseProxy.DomainLogic
{
    public class DestinationService
    {
        private readonly ILogger _logger;

        public DestinationService(ILogger<DestinationService> logger)
        {
            _logger = logger;
        }

        public IUpstreamServer? GetDestination(HttpRequestMessage httpRequestMessage)
        {
            IUpstreamServer? result = null;
            //TODO: make this actually look up where to send things using some config.
            result = new DataAccess.StaticContent.UpstreamServer(200, new Dictionary<string, string>(){ {"Content-Length", "13"}, {"Connection", "close"} }, "Hello, World!", _logger);

            return result;
        }
    }
}
