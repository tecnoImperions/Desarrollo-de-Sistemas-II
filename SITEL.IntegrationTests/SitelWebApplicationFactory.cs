using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SITEL.Server.Data;

namespace SITEL.IntegrationTests
{
    public class SitelWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"SitelIntegration_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                                d.ServiceType == typeof(DbContextOptions) ||
                                (d.ServiceType.IsGenericType &&
                                 d.ServiceType.GenericTypeArguments.Contains(typeof(AppDbContext)) &&
                                 d.ServiceType.Name.Contains("DbContextOptions")))
                    .ToList();

                foreach (var d in toRemove)
                {
                    services.Remove(d);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
            });
        }
    }
}
