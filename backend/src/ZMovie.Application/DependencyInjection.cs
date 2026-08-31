using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ZMovie.Application.Catalog;
using ZMovie.Application.Common;

namespace ZMovie.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddZMovieApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(ListTitlesQuery).Assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(ListTitlesQuery).Assembly);
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
