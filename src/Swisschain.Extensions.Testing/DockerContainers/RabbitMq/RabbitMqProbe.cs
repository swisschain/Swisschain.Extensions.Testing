using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Swisschain.Extensions.Testing.DockerContainers.Postgres;

namespace Swisschain.Extensions.Testing.DockerContainers.RabbitMq
{
    internal class RabbitMqProbe
    {
        private readonly string _containerIp;
        private readonly int _ampqPort;
        private readonly TimeSpan _initialWaitTime;
        private readonly TimeSpan _maxWaitTime;

        public RabbitMqProbe(string containerIp, int ampqPort, TimeSpan initialWaitTime, TimeSpan maxWaitTime)
        {
            _containerIp = containerIp;
            _ampqPort = ampqPort;
            _initialWaitTime = initialWaitTime;
            _maxWaitTime = maxWaitTime;
        }

        [DebuggerStepThrough]
        public async Task WaitUntilAvailable(CancellationToken cancellation)
        {
            await Task.Delay((int)_initialWaitTime.TotalMilliseconds, cancellation);

            var maxWaitTimeFromStart = DateTime.UtcNow.Add(_maxWaitTime);

            Exception lastException = null;
            while (DateTime.UtcNow < maxWaitTimeFromStart && !cancellation.IsCancellationRequested)
            {
                await Task.Delay(500, cancellation);

                try
                {
                    using var connection = new TcpClient();

                    await connection.ConnectAsync(_containerIp, _ampqPort);

                    if (connection.Connected)
                    {
                        return;
                    }
                }
                // TODO: Specific exception
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            throw new TimeoutException($"The {nameof(PostgresContainer)} instance did not become available in a timely fashion.", lastException);
        }
    }
}
