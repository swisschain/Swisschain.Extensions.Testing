using System;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;

namespace Swisschain.Extensions.Testing.DockerContainers.Postgres
{
    public class PostgresContainer
    {
        private readonly int _hostPort;
        private readonly string _mainDb;
        private readonly string _user;
        private readonly string _password;
        private readonly IContainerService _containerService;
        private readonly int _containerRpcPort = 5432;

        public PostgresContainer(string containerName = "", 
            int hostPort = 5432,
            string mainDb = "main_db",
            string user = "postgres", 
            string password = "pass",
            bool reuseIfExists = false,
            string version = "11.8-alpine")
        {
            _hostPort = hostPort;
            _mainDb = mainDb;
            _user = user;
            _password = password;

            var imageName = $"postgres:{version}";

            var builder = new Builder()
                .UseContainer()
                .WithName(containerName)
                .UseImage(imageName)
                .ExposePort(hostPort, _containerRpcPort)
                .WaitForPort($"{_containerRpcPort}/tcp", TimeSpan.FromMinutes(2))
                .WithEnvironment(
                    $"POSTGRES_DB={mainDb}",
                    $"POSTGRES_USER={user}",
                    $"POSTGRES_PASSWORD={password}");

            if (reuseIfExists)
            {
                builder.ReuseIfExists();
            }
            else
            {
                ContainerRemover.RemoveIfExists(containerName, imageName);
            }

            _containerService = builder.Build();
        }

        public string ContainerIp => _containerService.GetConfiguration().NetworkSettings.IPAddress;
        public string MainDbConnectionString => GetConnectionString(_mainDb);

        public async Task Start()
        {
            _containerService.Start();

            var probe = new PostgresProbe(MainDbConnectionString, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
            
            await probe.WaitUntilAvailable(CancellationToken.None);
        }

        public void Stop()
        {
            _containerService.Stop();
            _containerService.Remove();
        }

        public string GetConnectionString(string database)
        {
            return $"Server=localhost;Database={database};Port={_hostPort};User Id={_user};Password={_password};Ssl Mode=Disable;Pooling=false";
        }
    }
}
