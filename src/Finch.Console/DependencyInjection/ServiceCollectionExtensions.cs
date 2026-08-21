using Finch.Console.Domain;
using Finch.Console.Input;
using Finch.Console.Output;
using Finch.Console.Rules;
using Finch.Console.Statistics;
using Finch.Console.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Finch.Console.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers every component of the lending platform. Every public, non-abstract
    /// <see cref="ISpecification{T}"/> implementation in this assembly is registered automatically
    /// - see <see cref="RegisterImplementationsOf{TInterface}"/> - so adding a new rule class is
    /// enough; nothing here needs updating. Registration order is irrelevant regardless:
    /// <see cref="RulesEngine"/> sorts by each rule's <see cref="ISpecification{T}.Order"/>.
    /// </summary>
    public static IServiceCollection AddLendingPlatform(this IServiceCollection services)
    {
        RegisterImplementationsOf<ISpecification<LoanApplication>>(services);

        services.AddSingleton<RulesEngine>();
        services.AddSingleton<LoanApplicationFieldValidator>();
        services.AddSingleton<LoanApplicationStatistics>();
        services.AddSingleton<IConsoleReader, SystemConsoleReader>();
        services.AddSingleton<IConsoleWriter, SystemConsoleWriter>();
        services.AddSingleton<ApplicationResultPresenter>();
        services.AddSingleton<ConsoleLoanApplicationReader>();

        return services;
    }

    private static void RegisterImplementationsOf<TInterface>(IServiceCollection services)
    {
        var implementations = typeof(TInterface).Assembly
            .GetTypes()
            .Where(type => type is { IsInterface: false, IsAbstract: false, IsPublic: true }
                           && typeof(TInterface).IsAssignableFrom(type));

        foreach (var implementation in implementations)
            services.AddSingleton(typeof(TInterface), implementation);
    }
}