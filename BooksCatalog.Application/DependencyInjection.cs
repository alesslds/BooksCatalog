using BooksCatalog.Application.Repositories;
using BooksCatalog.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BooksCatalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookService, BookService>();

        return services;
    }
}
