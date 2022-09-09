using System.Collections.Concurrent;
using System.Threading.Tasks;
using Dapper;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Serilog;
using Serilog.Sinks.InMemory;
using Swisschain.Extensions.Testing.DockerContainers.Postgres;
using Xunit.Abstractions;

namespace Swisschain.Extensions.Testing.WebApplicationFactory
{
    public abstract class PostgresWebApplicationFactory<TStartup, TDatabaseContext> : WebApplicationFactory<TStartup> 
        where TStartup: class
        where TDatabaseContext : DbContext
    {
        private const string DefaultDbName = "sut";
        
        private readonly ConcurrentBag<NpgsqlConnection> _testDbConnections;
        
        protected PostgresWebApplicationFactory()
        {
            Container = new PostgresContainer(DefaultDbName, PortManager.GetNextPort());

            _testDbConnections = new ConcurrentBag<NpgsqlConnection>();
        }
        
        public PostgresContainer Container { get; }
        
        protected abstract string GetMigrationHistoryTableName();

        protected abstract string GetSchemaName();
        
        public string GetConnectionString(string dbName = DefaultDbName)
        {
            return Container.GetConnectionString(dbName);
        }

        public async Task<NpgsqlConnection> CreateConnection(string dbName = DefaultDbName, bool manageDisposing = false)
        {
            var connection = new NpgsqlConnection(GetConnectionString(dbName));

            await connection.OpenAsync();

            if (manageDisposing)
            {
                _testDbConnections.Add(connection);
            }

            return connection;
        }
        
        public DbContextOptions<TDatabaseContext> CreateDatabaseContextOptions()
        {
            return CreateDbContextOptionsBuilder(GetConnectionString(),
                    GetMigrationHistoryTableName(),
                    GetSchemaName(),
                    new NullLoggerFactory())
                .Options;
        }

        public void CreateTestDb(string name = DefaultDbName)
        {
            using var connection = new NpgsqlConnection(Container.MainDbConnectionString);

            connection.Execute($"create database {name}");
        }

        public void DropTestDb(string name = DefaultDbName)
        {
            foreach (var testDbConnection in _testDbConnections)
            {
                testDbConnection.Close();
                testDbConnection.Dispose();
            }

            _testDbConnections.Clear();

            using var connection = new NpgsqlConnection(Container.MainDbConnectionString);

            var query = @$"
                -- Disallow new connections
                update pg_database set datallowconn = 'false' where datname = '{name}';
                alter database {name} connection limit 1;
                -- Terminate existing connections
                select pg_terminate_backend(pid) from pg_stat_activity where datname = '{name}';
                -- Drop database
                drop database {name}";

            connection.Execute(query);
        }

        public GrpcChannel CreateGrpcChannel()
        {
            var options = new GrpcChannelOptions { HttpHandler = Server.CreateHandler() };
            return GrpcChannel.ForAddress( Server.BaseAddress, options);
        }
        
        public void ShowServerLogs(ITestOutputHelper outputHelper)
        {
            foreach (var logEvent in InMemorySink.Instance.LogEvents)
            {
                outputHelper.WriteLine($"[{logEvent.Level}]: {logEvent.RenderMessage()} {logEvent.Exception}");
            }
        }

        protected abstract override IHostBuilder CreateHostBuilder();
        
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.UseSerilog((ctx, conf) => conf.WriteTo.InMemory());

            Container.Start().GetAwaiter().GetResult();

            CreateTestDb();

            var connectionString = GetConnectionString();
            var migrationHistoryTableName = GetMigrationHistoryTableName();
            var schemaName = GetSchemaName();
        
            builder.ConfigureServices(x =>
            {
                x.RemoveDbContext<TDatabaseContext>();
                x.AddSingleton(CreateDbContextOptionsBuilder(connectionString,
                    migrationHistoryTableName,
                    schemaName,
                    new NullLoggerFactory()));
                
                // remove pending migrations checker
                x.RemoveDbSchemaValidationHost();
                
                ConfigureServicesEx(x);
            });
        }

        protected virtual void ConfigureServicesEx(IServiceCollection services)
        {
        }
        
        private static DbContextOptionsBuilder<TDatabaseContext> CreateDbContextOptionsBuilder(string connectionString,
            string migrationHistoryTableName,
            string schemaName,
            ILoggerFactory loggerFactory)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TDatabaseContext>();
            optionsBuilder.UseLoggerFactory(loggerFactory);

            optionsBuilder.UseNpgsql(connectionString,
                builder => builder.MigrationsHistoryTable(migrationHistoryTableName, schemaName));
            return optionsBuilder;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            DropTestDb();

            foreach (var connection in _testDbConnections)
            {
                connection.Dispose();
            }

            Container.Stop();
        }
    }
}
