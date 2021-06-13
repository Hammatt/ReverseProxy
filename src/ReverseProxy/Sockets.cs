using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReverseProxy.DomainLogic;
using ReverseProxy.Parser;

namespace ReverseProxy
{
    public class Sockets
    {
        private class StateObject
        {
            public const int BUFFER_SIZE = 1024;

            public byte[] Buffer { get; private set; }

            public StringBuilder StringBuilder { get; private set; }

            public Socket? WorkSocket { get; set; }

            public StateObject()
            {
                Buffer = new byte[BUFFER_SIZE];
                StringBuilder = new StringBuilder();
                WorkSocket = null;
            }
        }

        private const string BLANK_LINE = "\r\n\r\n";
        private readonly HttpRequestParser _httpRequestParser;
        private readonly HttpResponseParser _httpResponseParser;
        private readonly ILogger<Worker> _logger;
        private readonly ReverseProxyService _reverseProxyService;
        private readonly SemaphoreSlim _semaphore;

        public Sockets(HttpRequestParser httpRequestParser, HttpResponseParser httpResponseParser, ILogger<Worker> logger, ReverseProxyService reverseProxyService)
        {
            _httpRequestParser = httpRequestParser;
            _httpResponseParser = httpResponseParser;
            _logger = logger;
            _reverseProxyService = reverseProxyService;
            _semaphore = new SemaphoreSlim(0, 1);
        }

        public async Task StartListeningAsync(CancellationToken cancellationToken)
        {           
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000); //TODO: make configurable in appsettings

            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(localEndPoint);
            listener.Listen(100);

            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Waiting for a connection...");
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                await _semaphore.WaitAsync(cancellationToken);
            }
        }

        private void AcceptCallback(IAsyncResult asyncResult)
        {
            _semaphore.Release();

            if (asyncResult.AsyncState == null)
            {
                throw new ArgumentNullException(nameof(asyncResult.AsyncState));
            }
            Socket listener = (Socket) asyncResult.AsyncState;  
            Socket handler = listener.EndAccept(asyncResult);
    
            StateObject state = new StateObject();  
            state.WorkSocket = handler;  
            handler.BeginReceive(state.Buffer, 0, StateObject.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
        }

        private void ReadCallback(IAsyncResult asyncResult)
        {
            if (asyncResult.AsyncState == null)
            {
                throw new ArgumentNullException(nameof(asyncResult.AsyncState));
            }
            StateObject state = (StateObject) asyncResult.AsyncState;
            if (state.WorkSocket == null)
            {
                throw new ArgumentNullException(nameof(state.WorkSocket));
            }
            Socket handler = state.WorkSocket;


            string content = string.Empty;
    
            int bytesRead = handler.EndReceive(asyncResult);
            _logger.LogDebug("bytes read {0}", bytesRead);
    
            if (bytesRead > 0)
            {
                state.StringBuilder.Append(Encoding.ASCII.GetString(state.Buffer, 0, bytesRead));
    
                content = state.StringBuilder.ToString();
                if (content.IndexOf(BLANK_LINE) > -1)
                {
                    try
                    {
                        HttpRequestMessage? httpRequestMessage = _httpRequestParser.ParseHttpRequest(state.Buffer);

                        if (httpRequestMessage == null)
                        {
                            _logger.LogDebug("Responding with bad request");
                            SendBadRequestResponse(handler);
                        }
                        else
                        {
                            Task.Run(async () => {
                                try
                                {
                                    HttpResponseMessage? httpResponseMessage = await _reverseProxyService.ForwardMessageAsync(httpRequestMessage, CancellationToken.None);
                                    if (httpResponseMessage == null)
                                    {
                                        _logger.LogDebug("Responding with bad gateway");
                                        SendBadGatewayResponse(handler);
                                    }
                                    else
                                    {
                                        using (MemoryStream rawResponse = await _httpResponseParser.GetRawHttpResponseAsync(httpResponseMessage))
                                        {
                                            _logger.LogDebug("Relaying response from upstream server");
                                            Send(handler, rawResponse);
                                        }
                                    }
                                }
                                catch(Exception exception)
                                {
                                    _logger.LogError(exception.Message, exception);
                                    SendInternalServerErrorResponse(handler);
                                }
                            });
                        }
                    }
                    catch(Exception exception)
                    {
                        _logger.LogError(exception.Message, exception);
                        SendInternalServerErrorResponse(handler);
                    }
                }
                else
                {
                    handler.BeginReceive(state.Buffer, 0, StateObject.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);  
                }  
            }
        }

        private void Send(Socket handler, string data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);  
    
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);  
        }

        private void Send(Socket handler, MemoryStream data)
        {
            byte[] buffer = data.ToArray();
            _logger.LogTrace("Replying with {0}", Encoding.ASCII.GetString(buffer));
            handler.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult asyncResult)
        {
            try
            {
                if (asyncResult.AsyncState == null)
                {
                    throw new ArgumentNullException(nameof(asyncResult.AsyncState));
                }

                Socket handler = (Socket) asyncResult.AsyncState;
    
                int bytesSent = handler.EndSend(asyncResult);
    
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();  
    
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void SendBadRequestResponse(Socket handler)
        {
            Send(handler, "HTTP/1.1 400 Bad Request\r\n\r\n");
        }

        private void SendBadGatewayResponse(Socket handler)
        {
            Send(handler, "HTTP/1.1 502 Bad Gateway\r\n\r\n");
        }

        private void SendInternalServerErrorResponse(Socket handler)
        {
            Send(handler, "HTTP/1.1 50 Internal Server Error\r\n\r\n");
        }
    }
}
