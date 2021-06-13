using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ReverseProxy.Parser
{
    public class HttpRequestParserTests
    {
        private readonly HttpRequestParser _httpRequestParser;

        public HttpRequestParserTests()
        {
            _httpRequestParser = new HttpRequestParser(new NullLogger<HttpRequestParser>());
        }

        [Fact]
        public void TestParseSimpleRequest()
        {
            string rawHttpRequest = "GET / HTTP/1.1";
            byte[] bytes = Encoding.ASCII.GetBytes(rawHttpRequest);

            HttpRequestMessage? httpRequestMessage = _httpRequestParser.ParseHttpRequest(bytes);

            Assert.NotNull(httpRequestMessage);
            Assert.Equal(HttpMethod.Get, httpRequestMessage.Method);
            Assert.Equal(new Uri("http://localhost/"), httpRequestMessage.RequestUri);
            Assert.Equal(new Version("1.1"), httpRequestMessage.Version);
        }

        [Fact]
        public async Task TestParseComplexRequestAsync()
        {
            string rawHttpRequest = "POST / HTTP/1.1\r\nHost: localhost:11000\r\nUser-Agent: curl/7.76.1\r\nAccept: */*\r\nContent-Type: application/json\r\nContent-Length: 35\r\n\r\n{\"username\":\"xyz\",\"password\":\"xyz\"}";
            byte[] bytes = Encoding.ASCII.GetBytes(rawHttpRequest);

            HttpRequestMessage? httpRequestMessage = _httpRequestParser.ParseHttpRequest(bytes);

            Assert.NotNull(httpRequestMessage);
            Assert.Equal(HttpMethod.Post, httpRequestMessage.Method);
            Assert.Equal(new Uri("http://localhost/"), httpRequestMessage.RequestUri);
            Assert.Equal(new Version("1.1"), httpRequestMessage.Version);
            Assert.Equal("localhost:11000", httpRequestMessage.Headers.GetValues("Host").First());
            Assert.Equal("curl/7.76.1", httpRequestMessage.Headers.GetValues("User-Agent").First());
            Assert.Equal("*/*", httpRequestMessage.Headers.GetValues("Accept").First());
            Assert.Equal("application/json", httpRequestMessage.Content.Headers.GetValues("Content-Type").First());
            Assert.Equal("35", httpRequestMessage.Content.Headers.GetValues("Content-Length").First());
            Assert.Equal(@"{""username"":""xyz"",""password"":""xyz""}", await httpRequestMessage.Content.ReadAsStringAsync());
        }
    }
}
