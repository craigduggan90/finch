using Finch.Console.DependencyInjection;
using Finch.Console.Input;
using Finch.Console.Output;
using Finch.Console.Rules;
using Finch.Console.Statistics;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection().AddLendingPlatform();
await using var provider = services.BuildServiceProvider();

var reader = provider.GetRequiredService<ConsoleLoanApplicationReader>();
var rulesEngine = provider.GetRequiredService<RulesEngine>();
var statistics = provider.GetRequiredService<LoanApplicationStatistics>();
var presenter = provider.GetRequiredService<ApplicationResultPresenter>();

do
{
    var application = reader.ReadApplication();
    if (application is null)
        break;

    var decision = rulesEngine.Evaluate(application);

    statistics.Record(application, decision);
    presenter.Present(application, decision, statistics);
} while (reader.ShouldReadAnotherApplication());