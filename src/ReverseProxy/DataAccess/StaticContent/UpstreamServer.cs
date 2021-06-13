using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ReverseProxy.DataAccess.StaticContent
{
    public class UpstreamServer : IUpstreamServer
    {
        private readonly string _body;
        private readonly Dictionary<string, string> _headers;
        private readonly int _statusCode;
        private readonly ILogger _logger;

        public UpstreamServer(int statusCode, Dictionary<string, string> headers, string body, ILogger logger)
        {
            _body = body;
            _headers = headers;
            _statusCode = statusCode;
            _logger = logger;
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken)
        {
            HttpResponseMessage result = new HttpResponseMessage();

            result.StatusCode = (HttpStatusCode)_statusCode;
            result.Content = new StringContent(_body);
            foreach((string key, string value) in _headers)
            {
                //TODO: this has the same problem as the heep request parser...
                if (string.Equals("Content-Type", key, StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue(value);
                }
                else if (string.Equals("Content-Length", key, StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Content.Headers.ContentLength = long.Parse(value);
                }
                else
                {
                    result.Headers.Add(key, value);
                }
            }

            return Task.FromResult(result);
        }
    }
}
