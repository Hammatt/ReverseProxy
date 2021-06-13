using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace ReverseProxy.Parser
{
    public class HttpResponseParserTests
    {
        private readonly HttpResponseParser _httpResponseParser;

        public HttpResponseParserTests()
        {
            _httpResponseParser = new HttpResponseParser();
        }

        [Fact]
        public async Task TestParseSimpleResponse()
        {
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            httpResponseMessage.StatusCode = System.Net.HttpStatusCode.OK;

            string rawMessage;
            using (MemoryStream stream = await _httpResponseParser.GetRawHttpResponseAsync(httpResponseMessage))
            using (StreamReader reader = new StreamReader(stream))
            {
                rawMessage = await reader.ReadToEndAsync();
            }

            Assert.Equal("HTTP/1.1 200 OK\r\n\r\n", rawMessage);
        }

        [Fact]
        public async Task TestParseComplexResponse()
        {
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            httpResponseMessage.StatusCode = System.Net.HttpStatusCode.Forbidden;
            httpResponseMessage.Content = new StringContent("This is an example complex message!");
            httpResponseMessage.Headers.ETag = new EntityTagHeaderValue("\"abcd\"");
            CacheControlHeaderValue cacheControlHeaderValue = new CacheControlHeaderValue();
            cacheControlHeaderValue.MaxAge = TimeSpan.FromHours(3);
            httpResponseMessage.Headers.CacheControl = cacheControlHeaderValue;

            string rawMessage;
            using (MemoryStream stream = await _httpResponseParser.GetRawHttpResponseAsync(httpResponseMessage))
            using (StreamReader reader = new StreamReader(stream))
            {
                rawMessage = await reader.ReadToEndAsync();
            }

            Assert.Equal("HTTP/1.1 403 Forbidden\r\nETag:\"abcd\"\r\nCache-Control:max-age=10800\r\n\r\nThis is an example complex message!", rawMessage);
        }
    }
}