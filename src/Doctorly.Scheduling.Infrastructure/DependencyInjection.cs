using Doctorly.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Doctorly.Scheduling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SchedulingDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'SchedulingDatabase' is not configured.");

        services.AddDbContext<SchedulingDbContext>(options => options.UseSqlite(connectionString));

        return services;
    }
}
