using GloupUpRD.API.Data;
using Microsoft.EntityFrameworkCore;

namespace GloupUpRD.API.Extensions;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<GlowUpDbContext>(options =>
            options.UseNpgsql(connectionString));

        //services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        //services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
