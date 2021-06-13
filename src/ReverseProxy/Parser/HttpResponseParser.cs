using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ReverseProxy.Parser
{
    public class HttpResponseParser
    {
        public async Task<MemoryStream> GetRawHttpResponseAsync(HttpResponseMessage httpResponseMessage)
        {
            StringBuilder stringBuilder = new StringBuilder();

            AddStartLine(stringBuilder, httpResponseMessage.StatusCode);
            AddHeaders(stringBuilder, httpResponseMessage.Headers);

            byte[] bytes = Encoding.ASCII.GetBytes(stringBuilder.ToString());
            MemoryStream memoryStream = new MemoryStream();
            await memoryStream.WriteAsync(bytes);

            await AddBodyAsync(memoryStream, httpResponseMessage.Content);

            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream;
        }

        private async Task AddBodyAsync(MemoryStream memoryStream, HttpContent body)
        {
            await body.CopyToAsync(memoryStream);
        }

        private void AddHeaders(StringBuilder stringBuilder, HttpResponseHeaders headers)
        {
            foreach((string key, IEnumerable<string> value) in headers)
            {
                stringBuilder.Append(key);
                stringBuilder.Append(':');
                stringBuilder.Append(string.Join(';', value));
                stringBuilder.Append("\r\n");
            }
            stringBuilder.Append("\r\n");
        }

        private void AddStartLine(StringBuilder stringBuilder, HttpStatusCode statusCode)
        {
            stringBuilder.Append("HTTP/1.1 ");
            stringBuilder.Append((int)statusCode);
            stringBuilder.Append(' ');
            stringBuilder.Append(statusCode.ToString());
            stringBuilder.Append("\r\n");
        }
    }
}
