using Doctorly.Scheduling.Application.Common.Interfaces;
using Doctorly.Scheduling.Application.Notifications;
using Doctorly.Scheduling.Infrastructure.Notifications;
using Doctorly.Scheduling.Infrastructure.Persistence;
using Doctorly.Scheduling.Infrastructure.Persistence.Queries;
using Doctorly.Scheduling.Infrastructure.Persistence.Repositories;
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

        services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
        services.AddScoped<IAttendeeRepository, AttendeeRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventQueries, EventQueries>();

        services.Configure<NotificationOptions>(
            configuration.GetSection(NotificationOptions.SectionName));

        services.AddScoped<INotificationChannel, EmailNotificationChannel>();
        services.AddScoped<INotificationChannel, WhatsAppNotificationChannel>();
        services.AddScoped<INotificationChannel, PushNotificationChannel>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        return services;
    }
}
