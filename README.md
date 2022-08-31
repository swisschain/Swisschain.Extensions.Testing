# Swisschain.Extensions.Testing
Extensions for testing

## Instructions how to use PostgresWebApplicationFactory<TStartup, TDatabaseContext>:

This base class provides a convenient way to run services locally, with postgres DB
run in a local container, which in turn allows to run integration tests checking
controller logic or to check behaviour in an end-to-end way.

Since PostgresWebApplicationFactory<TStartup, TDatabaseContext> is an abstract class,
one has to provide concrete implementation for each endpoint to be tested.

For example:

```c#
    public class ServiceNamePostgresWebApplicationFactory<TStartup> : PostgresWebApplicationFactory<TStartup, DatabaseContext>
        where TStartup : class
        
        // DatabaseContext here is ServiceName-specific ef-core-based DbContext, which will be built upon pg-sql-container locally
    {
        protected override string GetMigrationHistoryTableName() => DatabaseContext.MigrationHistoryTable;

        protected override string GetSchemaName() => DatabaseContext.SchemaName;

        protected override IHostBuilder CreateHostBuilder()
        {
            var remoteSettingsData = new[]
            {
                "...",
                "...",
                "..."
            };
            return Program.CreateHostBuilder(new NullLoggerFactory(), remoteSettingsData);
            // alternatively one can provide settings explicitly
        }

        protected override void ConfigureServicesEx(IServiceCollection services)
        {
            // ef-core migrations can be explicitly enabled for some enpoints,
            // which don't have them in production
            services.AddHostedService<MigrationHost>();
            
            // by default all checks regarding migrations will be disabled
        }
    }
```

Having defined concrete base class, one will be able to use it as a fixture for tests:
```c#
public class SamplePostgresWebApplicationFactoryTests : IClassFixture<ServiceNamePostgresWebApplicationFactory<Startup>>
// Startup here is endpoint class for ServiceName
    {
        private readonly ServiceNamePostgresWebApplicationFactory<Startup> _factory;

        public SamplePostgresWebApplicationFactoryTests(ServiceNamePostgresWebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }
        
        [Theory]
        [InlineData("/api/somecontroller/somemethod")]
        public async Task Get_EndpointsReturnNonEmptySuccessAndCorrectContentType(string url)
        {
            // Arrange
            // creates http client
            var client = _factory.CreateClient();
            
            // Act
            // calls controller run locally via 
            var response = await client.GetAsync(url);
            
            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
            Assert.Equal("application/json; charset=utf-8", 
                response.Content.Headers.ContentType.ToString());
        }
    }
```

Please refer to the documentation on WebApplicationFactory-based integration test in AspNetCore
for information on way of configuring and troubleshooting the web part of this set up:
https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-5.0
