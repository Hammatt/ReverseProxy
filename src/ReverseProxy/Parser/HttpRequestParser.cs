using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ReverseProxy.Parser
{
    public class HttpRequestParser
    {
        private enum ParserState
        {
            Error = 0,
            StartLine = 1,
            Headers = 2,
            Body = 3,
        }

        private readonly ILogger _logger;

        public HttpRequestParser(ILogger<HttpRequestParser> logger)
        {
            _logger = logger;
        }

        public HttpRequestMessage? ParseHttpRequest(byte[] request)
        {
            HttpRequestMessage? result = new HttpRequestMessage();
            StringBuilder content = new StringBuilder();
            Dictionary<string, string> contentHeaders = new Dictionary<string, string>();

            string rawRequest = Encoding.ASCII.GetString(request);
            string[] rawRequestLines = rawRequest.Split("\r\n");

            ParserState parserState = ParserState.StartLine;

            foreach(string rawRequestLine in rawRequestLines)
            {
                switch(parserState)
                {
                    case ParserState.Error:
                        break;
                    case ParserState.StartLine:
                        string[] rawStartLine = rawRequestLine.Split(' ');
                        if (rawStartLine.Length == 3)
                        {
                            result.Method = new HttpMethod(rawStartLine[0]);
                            result.RequestUri = new Uri(new Uri("http://localhost"), rawStartLine[1]);//TODO fix localhost
                            result.Version = new Version(rawStartLine[2].Substring(5));

                            parserState = ParserState.Headers;
                        }
                        else
                        {
                            parserState = ParserState.Error;
                        }
                        break;
                    case ParserState.Headers:
                        if (rawRequestLine == string.Empty)
                        {
                            parserState = ParserState.Body;
                        }
                        else
                        {
                            string[] headerParts = rawRequestLine.Split(':', 2, StringSplitOptions.TrimEntries);
                            if (headerParts.Length == 2)
                            {
                                string key = headerParts[0];
                                string[] values = headerParts[1].Split(";");
                                if (key.StartsWith("content", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    contentHeaders.Add(key, values[0]);
                                }
                                else
                                {
                                    result.Headers.Add(key, values);
                                }
                            }
                            else
                            {
                                parserState = ParserState.Error;
                            }
                        }
                        break;
                    case ParserState.Body:
                        content.Append(rawRequestLine);//TODO check what happens here if we have a body that happens to have linebreaks.
                        break;
                }

                if (parserState == ParserState.Error)
                {
                    _logger.LogError("Parser in error state, cannot continue on line {0}", rawRequestLine);
                    result = null;
                    break;
                }
            }

            if (result != null)
            {
                result.Content = new StringContent(content.ToString());
                foreach((string key, string value) in contentHeaders)
                {
                    //TODO: fix this to make it support more headers
                    if (string.Equals("Content-Type", key, StringComparison.InvariantCultureIgnoreCase))
                    {
                        result.Content.Headers.ContentType = new MediaTypeHeaderValue(value);
                    }
                    else if (string.Equals("Content-Length", key, StringComparison.InvariantCultureIgnoreCase))
                    {
                        result.Content.Headers.ContentLength = long.Parse(value);
                    }
                }
            }
            return result;
        }
    }
}
