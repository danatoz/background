using Background.Dal.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Background.Dal;

public static class DalRegistration
{
    public static IServiceCollection AddDal(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(
                connectionString,
                o => o.MapEnum<Entities.MessageStatus>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IInboxMessageRepository, InboxMessageRepository>();

        return services;
    }
}
