using System.Collections.Concurrent;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Swisschain.Extensions.Testing.DockerContainers.Postgres;
using Xunit;

namespace Swisschain.Extensions.Testing
{
    public class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgresContainer _container;
        private readonly ConcurrentBag<NpgsqlConnection> _testDbConnections;

        public PostgresFixture(string postgresContainerName = "tests-pg")
        {
            _container = new PostgresContainer(postgresContainerName, PortManager.GetNextPort());
            _testDbConnections = new ConcurrentBag<NpgsqlConnection>();
        }

        public async Task<NpgsqlConnection> CreateConnection(bool manageDisposing = false)
        {
            var connection = new NpgsqlConnection(_container.GetConnectionString("test_db"));

            await connection.OpenAsync();

            if (manageDisposing)
            {
                _testDbConnections.Add(connection);
            }

            return connection;
        }

        public async Task CreateTestDb()
        {
            await using var connection = new NpgsqlConnection(_container.MainDbConnectionString);

            await connection.ExecuteAsync("create database test_db");
        }

        public async Task DropTestDb()
        {
            foreach (var testDbConnection in _testDbConnections)
            {
                await testDbConnection.CloseAsync();
                await testDbConnection.DisposeAsync();
            }

            _testDbConnections.Clear();

            await using var connection = new NpgsqlConnection(_container.MainDbConnectionString);

            var query = @"
                -- Disallow new connections
                update pg_database set datallowconn = 'false' where datname = 'test_db';
                alter database test_db connection limit 1;

                -- Terminate existing connections
                select pg_terminate_backend(pid) from pg_stat_activity where datname = 'test_db';

                -- Drop database
                drop database test_db";

            await connection.ExecuteAsync(query);
        }

        public async Task InitializeAsync()
        {
            await _container.Start();
        }

        public async Task DisposeAsync()
        {
            foreach (var connection in _testDbConnections)
            {
                await connection.DisposeAsync();
            }

            _container.Stop();
        }
    }
}
