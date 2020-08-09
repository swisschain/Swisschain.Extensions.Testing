using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Swisschain.Extensions.Testing.DockerContainers.Postgres
{
    internal class PostgresProbe
    {
        private readonly string _connectionString;
        private readonly TimeSpan _initialWaitTime;
        private readonly TimeSpan _maxWaitTime;

        public PostgresProbe(string connectionString, TimeSpan initialWaitTime, TimeSpan maxWaitTime)
        {
            _connectionString = connectionString;
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
                    await using var connection = new NpgsqlConnection(_connectionString);
                    await connection.OpenAsync(cancellation);
                    await connection.ExecuteAsync("select version()");

                    return;
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
