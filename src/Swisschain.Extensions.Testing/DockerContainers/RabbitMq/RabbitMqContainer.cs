using System;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Swisschain.Extensions.Testing.DockerContainers.Postgres;

namespace Swisschain.Extensions.Testing.DockerContainers.RabbitMq
{
    public class RabbitMqContainer
    {
        private readonly IContainerService _containerService;
        private readonly int _containerAmpqPort = 5672;
        private readonly int _containerManagementPort = 15672;

        public RabbitMqContainer(string containerName = "", 
            int hostAmpqPort = 5672,
            int hostManagementPort = 15672,
            string user = "rabbit", 
            string password = "pass",
            bool reuseIfExists = false,
            string version = "3.8.6-management-alpine")
        {
            HostAmpqPort = hostAmpqPort;
            HostManagementPort = hostManagementPort;
            User = user;
            Password = password;

            var imageName = $"rabbitmq:{version}";

            var builder = new Builder()
                .UseContainer()
                .WithName(containerName)
                .UseImage(imageName)
                .ExposePort(hostAmpqPort, _containerAmpqPort)
                .ExposePort(hostManagementPort, _containerManagementPort)
                .WaitForPort($"{_containerAmpqPort}/tcp", TimeSpan.FromMinutes(2))
                .WithEnvironment(
                    $"RABBITMQ_DEFAULT_USER={user}",
                    $"RABBITMQ_DEFAULT_PASS={password}");

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

        public int HostAmpqPort { get; }
        public int HostManagementPort { get; }
        public string User { get; }
        public string Password { get; }
        public string ContainerIp => _containerService.GetConfiguration().NetworkSettings.IPAddress;
        public string AmpqUrl => $"rabbitmq://{ContainerIp}:{HostAmpqPort}";
        public string ManagementUrl => $"http://{ContainerIp}:{HostManagementPort}";

        public async Task Start()
        {
            _containerService.Start();

            var probe = new RabbitMqProbe(ContainerIp, HostAmpqPort, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
            
            await probe.WaitUntilAvailable(CancellationToken.None);
        }

        public void Stop()
        {
            _containerService.Stop();
            _containerService.Remove();
        }
    }
}
