using Finch.Console.Domain;
using Finch.Console.Input;
using Finch.Console.Output;
using Finch.Console.Rules;
using Finch.Console.Rules.Specifications;
using Finch.Console.Statistics;
using Finch.Console.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Finch.Console.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers every component of the lending platform. Rule registration order determines
    /// evaluation order in <see cref="RulesEngine"/> - see CLAUDE.md "Future considerations".
    /// </summary>
    public static IServiceCollection AddLendingPlatform(this IServiceCollection services)
    {
        services.AddSingleton<ISpecification<LoanApplication>, MinimumLoanAmountSpecification>();
        services.AddSingleton<ISpecification<LoanApplication>, MaximumLoanAmountSpecification>();
        services.AddSingleton<ISpecification<LoanApplication>, HighValueLtvSpecification>();
        services.AddSingleton<ISpecification<LoanApplication>, HighValueCreditScoreSpecification>();
        services.AddSingleton<ISpecification<LoanApplication>, LowValueMaximumLtvSpecification>();
        services.AddSingleton<ISpecification<LoanApplication>, LowValueLtvBand1CreditScoreSpecification>();
        services.AddSingleton<ISpecification<LoanApplication>, LowValueLtvBand2CreditScoreSpecification>();
        services.AddSingleton<ISpecification<LoanApplication>, LowValueLtvBand3CreditScoreSpecification>();

        services.AddSingleton<RulesEngine>();
        services.AddSingleton<LoanApplicationFieldValidator>();
        services.AddSingleton<LoanApplicationStatistics>();
        services.AddSingleton<IConsoleWriter, SystemConsoleWriter>();
        services.AddSingleton<ApplicationResultPresenter>();
        services.AddSingleton<ConsoleLoanApplicationReader>();

        return services;
    }
}