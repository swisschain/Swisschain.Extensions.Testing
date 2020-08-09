using System.Threading.Tasks;
using Swisschain.Extensions.Testing.DockerContainers.RabbitMq;
using Xunit;

namespace Swisschain.Extensions.Testing
{
    public class RabbitMqFixture : IAsyncLifetime
    {
        private readonly RabbitMqContainer _container;
        
        public RabbitMqFixture(string rabbitContainerName = "tests-rabbit")
        {
            _container = new RabbitMqContainer(rabbitContainerName, PortManager.GetNextPort(), PortManager.GetNextPort());
        }

        public string AmpqUrl => _container.AmpqUrl;
        public string User => _container.User;
        public string Password => _container.Password;

        public async Task InitializeAsync()
        {
            await _container.Start();
        }

        public Task DisposeAsync()
        {
            _container.Stop();

            return Task.CompletedTask;
        }
    }
}
