using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ReverseProxy
{
    public class Worker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly Sockets _sockets;

        public Worker(ILogger<Worker> logger, Sockets sockets)
        {
            _logger = logger;
            _sockets = sockets;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _sockets.StartListeningAsync(cancellationToken);
                }
                catch(Exception exception)
                {
                    _logger.LogError(exception.Message, exception);
                }
            }
        }

        
    }
}
