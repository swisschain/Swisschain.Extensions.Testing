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

        async Task IAsyncLifetime.InitializeAsync()
        {
            await _container.Start();

            await InitializeAsync();
        }

        protected virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await DisposeAsync();

            _container.Stop();
        }

        protected virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
