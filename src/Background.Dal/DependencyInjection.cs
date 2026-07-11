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
                o => o.MapEnum<Entities.JobStatus>("message_status")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IProcessingJobRepository, ProcessingJobRepository>();

        return services;
    }
}
