using Finch.Console.Domain;
using Finch.Console.Rules;

namespace Finch.Console.UnitTests.Rules;

public static class RulesEngineTests
{
    public abstract class RulesEngineTestsBase()
    {
        protected static readonly LoanApplication AnyApplication = new(500_000, 1_000_000, 800);

        protected sealed class FakeSpecification(bool applicable, bool satisfied, string description, int order = 0)
            : ISpecification<LoanApplication>
        {
            public int IsSatisfiedByCallCount { get; private set; }

            public int Order => order;

            public string Description => description;

            public bool IsApplicableTo(LoanApplication item) => applicable;

            public bool IsSatisfiedBy(LoanApplication item)
            {
                IsSatisfiedByCallCount++;
                return satisfied;
            }
        }
    }

    public class Evaluate() : RulesEngineTestsBase
    {
        [Fact]
        public void ShouldReturnApproved_WhenAllApplicableRulesAreSatisfied()
        {
            var engine = new RulesEngine([
                new FakeSpecification(applicable: true, satisfied: true, description: "rule-1", order: 1),
                new FakeSpecification(applicable: true, satisfied: true, description: "rule-2", order: 2),
            ]);

            var decision = engine.Evaluate(AnyApplication);

            Assert.Equal(DecisionOutcome.Approved, decision.Outcome);
            Assert.Null(decision.Reason);
        }

        [Fact]
        public void ShouldReturnApproved_WhenTheOnlyFailingRuleIsNotApplicable()
        {
            var engine = new RulesEngine([
                new FakeSpecification(applicable: false, satisfied: false, description: "should-be-skipped"),
            ]);

            var decision = engine.Evaluate(AnyApplication);

            Assert.Equal(DecisionOutcome.Approved, decision.Outcome);
        }

        [Fact]
        public void ShouldReturnFirstFailingRuleDescription_AndStopEvaluating_WhenAnApplicableRuleFails()
        {
            var thirdRule = new FakeSpecification(applicable: true, satisfied: false, description: "third-rule", order: 3);
            var engine = new RulesEngine([
                new FakeSpecification(applicable: true, satisfied: true, description: "first-rule", order: 1),
                new FakeSpecification(applicable: true, satisfied: false, description: "second-rule", order: 2),
                thirdRule,
            ]);

            var decision = engine.Evaluate(AnyApplication);

            Assert.Equal(DecisionOutcome.Declined, decision.Outcome);
            Assert.Equal("second-rule", decision.Reason);
            Assert.Equal(0, thirdRule.IsSatisfiedByCallCount);
        }

        [Fact]
        public void ShouldEvaluateByOrderProperty_RegardlessOfTheOrderRulesWereSupplied()
        {
            var lastByOrder = new FakeSpecification(applicable: true, satisfied: false, description: "order-3-rule", order: 3);

            // Supplied out of Order sequence - the engine must still evaluate by Order, not input position.
            var engine = new RulesEngine([
                lastByOrder,
                new FakeSpecification(applicable: true, satisfied: false, description: "order-2-rule", order: 2),
                new FakeSpecification(applicable: true, satisfied: true, description: "order-1-rule", order: 1),
            ]);

            var decision = engine.Evaluate(AnyApplication);

            Assert.Equal(DecisionOutcome.Declined, decision.Outcome);
            Assert.Equal("order-2-rule", decision.Reason);
            Assert.Equal(0, lastByOrder.IsSatisfiedByCallCount);
        }
    }
}