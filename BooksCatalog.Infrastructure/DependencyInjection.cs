using System;
using BooksCatalog.Application.Repositories;
using BooksCatalog.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BooksCatalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Provider de connection string (Secrets Manager + fallback a appsettings)
        services.AddSingleton<IDbConnectionStringProvider, SecretsManagerConnectionStringProvider>();
        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            var provider = sp.GetRequiredService<IDbConnectionStringProvider>();
            var connectionString = provider.GetConnectionString();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string is not configured.");
            }

            return NpgsqlDataSource.Create(connectionString);
        });

        services.AddScoped<IBookRepository, BookRepository>();

        return services;
    }
}
