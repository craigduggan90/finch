using Finch.Console.DependencyInjection;
using Finch.Console.Domain;
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
    var request = reader.ReadApplication();
    if (request is null)
        break;

    var application = LoanApplication.FromRequest(request);
    var decision = rulesEngine.Evaluate(application);

    statistics.Record(application, decision);
    presenter.Present(application, decision, statistics);
} while (reader.ShouldReadAnotherApplication());