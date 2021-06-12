using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ReverseProxy
{
    public class Worker : BackgroundService
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

        private readonly ILogger<Worker> _logger;
        private readonly SemaphoreSlim _semaphore;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _semaphore = new SemaphoreSlim(0, 1);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                    IPAddress ipAddress = ipHostInfo.AddressList[0];
                    IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000); //TODO: make configurable in appsettings

                    Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    listener.Bind(localEndPoint);
                    listener.Listen(100);

                    while (true)
                    {
        
                        Console.WriteLine("Waiting for a connection...");
                        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
        
                        await _semaphore.WaitAsync(cancellationToken);
                    }  
                }
                catch(Exception exception)
                {
                    _logger.LogError(exception.Message, exception);
                }
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            _semaphore.Release();
    
            Socket listener = (Socket) ar.AsyncState;  
            Socket handler = listener.EndAccept(ar);
    
            StateObject state = new StateObject();  
            state.WorkSocket = handler;  
            handler.BeginReceive(state.Buffer, 0, StateObject.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
        }

        public void ReadCallback(IAsyncResult ar)
        {
            String content = string.Empty;
    
            StateObject state = (StateObject) ar.AsyncState;
            Socket handler = state.WorkSocket;
    
            int bytesRead = handler.EndReceive(ar);
            _logger.LogDebug("bytes read {0}", bytesRead);
    
            if (bytesRead > 0)
            {
                state.StringBuilder.Append(Encoding.ASCII.GetString(state.Buffer, 0, bytesRead));
    
                content = state.StringBuilder.ToString();
                _logger.LogDebug("current content {0}", content);
                if (content.IndexOf("\r\n") > -1)
                {
                    _logger.LogDebug("Found CRLF");
                    _logger.LogInformation("Read {0} bytes from socket. \n Data : {1}", content.Length, content);
                    //TODO: read body...

                    Send(handler, "HTTP/1.1 200 OK\r\nContent-Length: 13\r\nConnection: close\r\n\r\nHello, world!");
                }
                else
                {
                    handler.BeginReceive(state.Buffer, 0, StateObject.BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);  
                }  
            }
            else
            {
                _logger.LogInformation("Read {0} bytes from socket. \n Data : {1}", content.Length, content);
            }
        }

        private void Send(Socket handler, String data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);  
    
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);  
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket) ar.AsyncState;
    
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);
    
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();  
    
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
