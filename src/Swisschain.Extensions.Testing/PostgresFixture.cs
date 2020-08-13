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
        private readonly ConcurrentBag<NpgsqlConnection> _testDbConnections;

        public PostgresFixture(string postgresContainerName = "tests-pg")
        {
            Container = new PostgresContainer(postgresContainerName, PortManager.GetNextPort());

            _testDbConnections = new ConcurrentBag<NpgsqlConnection>();
        }

        public PostgresContainer Container { get; }

        public string GetConnectionString(string dbName = "test_db")
        {
            return Container.GetConnectionString(dbName);
        }

        public async Task<NpgsqlConnection> CreateConnection(string dbName = "test_db", bool manageDisposing = false)
        {
            var connection = new NpgsqlConnection(GetConnectionString(dbName));

            await connection.OpenAsync();

            if (manageDisposing)
            {
                _testDbConnections.Add(connection);
            }

            return connection;
        }

        public async Task CreateTestDb(string name = "test_db")
        {
            await using var connection = new NpgsqlConnection(Container.MainDbConnectionString);

            await connection.ExecuteAsync($"create database {name}");
        }

        public async Task DropTestDb(string name = "test_db")
        {
            foreach (var testDbConnection in _testDbConnections)
            {
                await testDbConnection.CloseAsync();
                await testDbConnection.DisposeAsync();
            }

            _testDbConnections.Clear();

            await using var connection = new NpgsqlConnection(Container.MainDbConnectionString);

            var query = @$"
                -- Disallow new connections
                update pg_database set datallowconn = 'false' where datname = '{name}';
                alter database {name} connection limit 1;

                -- Terminate existing connections
                select pg_terminate_backend(pid) from pg_stat_activity where datname = '{name}';

                -- Drop database
                drop database {name}";

            await connection.ExecuteAsync(query);
        }

        async Task IAsyncLifetime.InitializeAsync()
        {
            await Container.Start();

            await InitializeAsync();
        }

        protected virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await DisposeAsync();

            foreach (var connection in _testDbConnections)
            {
                await connection.DisposeAsync();
            }

            Container.Stop();
        }

        protected virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
