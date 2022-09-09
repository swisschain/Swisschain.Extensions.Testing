using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Swisschain.Extensions.Testing
{
    public static class ServiceCollectionExtensions
    {
        public static void RemoveDbContext<T>(this IServiceCollection services) where T : DbContext
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<T>));
            if (descriptor != null) services.Remove(descriptor);
        }
        
        public static void RemoveDbSchemaValidationHost(this IServiceCollection services)
        {
            var descriptor = services.SingleOrDefault(d => d.ImplementationType?.FullName?.Contains("DbSchemaValidationHost") == true);
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
        }
    }
}
