using GlowUpRD.API.Data;
using Microsoft.EntityFrameworkCore;

namespace GlowUpRD.API.Extensions;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is required. Configure it with dotnet user-secrets.");

        services.AddDbContext<GlowUpDbContext>(options =>
            options.UseNpgsql(connectionString));

        //services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        //services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
