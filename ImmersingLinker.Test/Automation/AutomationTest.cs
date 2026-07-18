using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Enums.Automation;
using ImmersingLinker.Core.Exceptions.Automations;
using ImmersingLinker.Core.Models.Automation;
using Action = ImmersingLinker.Core.Abstractions.Automation.Action;

namespace ImmersingLinker.Test.Automation;

public class AutomationTest
{
    private static readonly Guid TestPlanGuid = Guid.NewGuid();
    private const string TestPlanName = "TestPlan";

    // ===== Test Stubs =====

    private class TestTrigger : Trigger, IManualTrigger
    {
        public void Fire(TriggerFiredEventArgs args)
        {
            OnTriggerFired(this, args);
        }
    }

    private class TestQueryTrigger : Trigger, IQueryNecessaryTrigger
    {
        private readonly Func<Task<bool>> _checkFunc;

        public TestQueryTrigger(Func<Task<bool>> checkFunc, TimeSpan? interval = null)
        {
            _checkFunc = checkFunc;
            PollingInterval = interval ?? TimeSpan.FromSeconds(1);
        }

        public TimeSpan PollingInterval { get; }

        public Task<bool> CheckConditionAsync()
        {
            return _checkFunc();
        }
    }

    private class TestRule : Rule
    {
        private readonly bool _satisfied;

        public TestRule(bool satisfied)
        {
            _satisfied = satisfied;
        }

        public override bool IsSatisfied() => _satisfied;
    }

    private class TestAction : Action
    {
        private readonly Func<Task>? _onInvoke;
        private readonly Func<Task>? _onRevert;
        private readonly Func<Exception, Task>? _onRevertFailed;
        private readonly bool _revertable;

        public bool InvokeCalled { get; private set; }
        public bool RevertCalled { get; private set; }
        public int InvokeCount { get; private set; }
        public int RevertCount { get; private set; }

        public TestAction(bool revertable = true,
            Func<Task>? onInvoke = null,
            Func<Task>? onRevert = null,
            Func<Exception, Task>? onRevertFailed = null)
        {
            _revertable = revertable;
            _onInvoke = onInvoke;
            _onRevert = onRevert;
            _onRevertFailed = onRevertFailed;
        }

        public override bool Revertable => _revertable;

        public override Task OnInvoke()
        {
            InvokeCalled = true;
            InvokeCount++;
            return _onInvoke?.Invoke() ?? Task.CompletedTask;
        }

        public override Task OnRevert()
        {
            RevertCalled = true;
            RevertCount++;
            return _onRevert?.Invoke() ?? Task.CompletedTask;
        }

        public override Task OnRevertFailed(Exception e)
        {
            return _onRevertFailed?.Invoke(e) ?? Task.CompletedTask;
        }
    }

    private static RuleSet CreateRuleSet(RuleSetSatisfyMode mode, params RuleBase[] rules)
    {
        var ruleSet = new RuleSet
        {
            SatisfyMode = mode
        };
        foreach (var rule in rules)
            ruleSet.AddRule(rule);
        return ruleSet;
    }

    private static AutomationPlan CreateTestPlan(
        Trigger? trigger = null,
        RuleSet? ruleSet = null,
        List<Action>? actions = null,
        bool revertable = false,
        Guid? guid = null)
    {
        return new AutomationPlan
        {
            Guid = guid ?? TestPlanGuid,
            Name = TestPlanName,
            Revertable = revertable,
            Trigger = trigger ?? new TestTrigger(),
            RuleSet = ruleSet,
            Actions = actions ?? []
        };
    }

    // ===== RuleSet =====

    [Fact]
    public void RuleSet_AllSatisfied_WhenAllRulesSatisfied_ReturnsTrue()
    {
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(true), new TestRule(true));

        Assert.True(ruleSet.IsSatisfied());
    }

    [Fact]
    public void RuleSet_AllSatisfied_WhenOneRuleFails_ReturnsFalse()
    {
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(true), new TestRule(false));

        Assert.False(ruleSet.IsSatisfied());
    }

    [Fact]
    public void RuleSet_AllSatisfied_WhenAllRulesFail_ReturnsFalse()
    {
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(false), new TestRule(false));

        Assert.False(ruleSet.IsSatisfied());
    }

    [Fact]
    public void RuleSet_AnySatisfied_WhenOneRuleSatisfied_ReturnsTrue()
    {
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AnySatisfied, new TestRule(false), new TestRule(true));

        Assert.True(ruleSet.IsSatisfied());
    }

    [Fact]
    public void RuleSet_AnySatisfied_WhenAllRulesSatisfied_ReturnsTrue()
    {
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AnySatisfied, new TestRule(true), new TestRule(true));

        Assert.True(ruleSet.IsSatisfied());
    }

    [Fact]
    public void RuleSet_AnySatisfied_WhenAllRulesFail_ReturnsFalse()
    {
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AnySatisfied, new TestRule(false), new TestRule(false));

        Assert.False(ruleSet.IsSatisfied());
    }

    [Fact]
    public void RuleSet_Not_InvertsResult()
    {
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(true), new TestRule(true));
        ruleSet.Not = true;

        Assert.False(ruleSet.IsSatisfied());
    }

    [Fact]
    public void RuleSet_EmptyRules_AllSatisfied_ReturnsTrue()
    {
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied);

        Assert.True(ruleSet.IsSatisfied());
    }

    [Fact]
    public void RuleSet_EmptyRules_AnySatisfied_ReturnsFalse()
    {
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AnySatisfied);

        Assert.False(ruleSet.IsSatisfied());
    }

    [Fact]
    public void RuleSet_AddRule_AddsToCollection()
    {
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied);
        var rule = new TestRule(true) { Guid = Guid.NewGuid() };

        ruleSet.AddRule(rule);

        Assert.Single(ruleSet.Rules);
    }

    [Fact]
    public void RuleSet_AddRule_DuplicateGuid_ThrowsArgumentException()
    {
        var guid = Guid.NewGuid();
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(true) { Guid = guid });

        Assert.Throws<ArgumentException>(() => ruleSet.AddRule(new TestRule(false) { Guid = guid }));
    }

    [Fact]
    public void RuleSet_RemoveRule_RemovesFromCollection()
    {
        var guid = Guid.NewGuid();
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(true) { Guid = guid });

        ruleSet.RemoveRule(guid);

        Assert.Empty(ruleSet.Rules);
    }

    [Fact]
    public void RuleSet_RemoveRule_NonexistentGuid_DoesNothing()
    {
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(true) { Guid = Guid.NewGuid() });

        ruleSet.RemoveRule(Guid.NewGuid());

        Assert.Single(ruleSet.Rules);
    }

    [Fact]
    public void RuleSet_NestedRuleSet_AllSatisfied()
    {
        var inner = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(true));
        var outer = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, inner, new TestRule(true));

        Assert.True(outer.IsSatisfied());
    }

    [Fact]
    public void RuleSet_NestedRuleSet_InnerFails()
    {
        var inner = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(false));
        var outer = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, inner, new TestRule(true));

        Assert.False(outer.IsSatisfied());
    }

    // ===== AutomationRunner =====

    [Fact]
    public async Task AutomationRunner_ExecuteAsync_Forward_InvokesAllActions()
    {
        var action1 = new TestAction();
        var action2 = new TestAction();
        var runner = new AutomationRunner(Guid.NewGuid(), false, [action1, action2]);

        await runner.ExecuteAsync();

        Assert.True(action1.InvokeCalled);
        Assert.True(action2.InvokeCalled);
        Assert.Equal(1, action1.InvokeCount);
        Assert.Equal(1, action2.InvokeCount);
    }

    [Fact]
    public async Task AutomationRunner_ExecuteAsync_Forward_SetsCurrentStep()
    {
        var actions = new List<Action> { new TestAction(), new TestAction(), new TestAction() };
        var runner = new AutomationRunner(Guid.NewGuid(), false, actions);

        await runner.ExecuteAsync();

        Assert.Equal(2, runner.CurrentStep);
    }

    [Fact]
    public async Task AutomationRunner_ExecuteAsync_RevertMode_RevertsActions()
    {
        var action1 = new TestAction();
        var action2 = new TestAction();
        var forwardRunner = new AutomationRunner(Guid.NewGuid(), false, [action1, action2]);

        await forwardRunner.ExecuteAsync();
        var runner = forwardRunner.CreateRevertRunner();

        await runner.ExecuteAsync();

        Assert.True(action1.RevertCalled);
        Assert.True(action2.RevertCalled);
    }

    [Fact]
    public async Task AutomationRunner_ExecuteAsync_RevertMode_SkipsNonRevertable()
    {
        var action1 = new TestAction(revertable: true);
        var action2 = new TestAction(revertable: false);
        var forwardRunner = new AutomationRunner(Guid.NewGuid(), false, [action1, action2]);

        await forwardRunner.ExecuteAsync();
        var runner = forwardRunner.CreateRevertRunner();

        await runner.ExecuteAsync();

        Assert.True(action1.RevertCalled);
        Assert.False(action2.RevertCalled);
    }

    [Fact]
    public async Task AutomationRunner_ExecuteAsync_FiresEvents_InOrder()
    {
        var events = new List<string>();
        var action = new TestAction();
        var runner = new AutomationRunner(Guid.NewGuid(), false, [action]);

        runner.Started += (_, _) => events.Add("Started");
        runner.Stopped += (_, _) => events.Add("Stopped");
        runner.Completed += (_, _) => events.Add("Completed");

        await runner.ExecuteAsync();

        Assert.Equal(["Started", "Stopped", "Completed"], events);
    }

    [Fact]
    public async Task AutomationRunner_ExecuteAsync_ActionThrows_FiresFailedAndStopped()
    {
        var expectedException = new InvalidOperationException("test");
        var action = new TestAction(onInvoke: () => throw expectedException);
        var runner = new AutomationRunner(Guid.NewGuid(), false, [action]);

        RunnerFailedEventArgs? failedArgs = null;
        var events = new List<string>();
        runner.Stopped += (_, _) => events.Add("Stopped");
        runner.Failed += (_, args) => { events.Add("Failed"); failedArgs = args; };

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteAsync());

        Assert.Contains("Stopped", events);
        Assert.Contains("Failed", events);
        Assert.NotNull(failedArgs);
        Assert.Same(expectedException, failedArgs!.Exception);
        Assert.Same(runner, failedArgs.Runner);
    }

    [Fact]
    public async Task AutomationRunner_ExecuteAsync_EmptyActions_Completes()
    {
        var runner = new AutomationRunner(Guid.NewGuid(), false, []);

        await runner.ExecuteAsync();

        Assert.Equal(-1, runner.CurrentStep);
    }

    [Fact]
    public async Task AutomationRunner_ExecuteAsync_Cancelled_ThrowsOperationCanceled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var action = new TestAction();
        var runner = new AutomationRunner(Guid.NewGuid(), false, [action]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.ExecuteAsync(cts.Token));
    }

    [Fact]
    public void AutomationRunner_CreateRevertRunner_CopiesCurrentStep()
    {
        var actions = new List<Action> { new TestAction(), new TestAction() };
        var runner = new AutomationRunner(Guid.NewGuid(), false, actions);

        var revertRunner = runner.CreateRevertRunner();

        Assert.True(revertRunner.RevertMode);
        Assert.Equal(runner.CurrentStep, revertRunner.CurrentStep);
        Assert.Equal(actions.Count, revertRunner.Actions.Count);
    }

    [Fact]
    public void AutomationRunner_CreateRevertRunner_CreatesShallowCopyOfActionsList()
    {
        var action = new TestAction();
        var runner = new AutomationRunner(Guid.NewGuid(), false, [action]);

        var revertRunner = runner.CreateRevertRunner();

        Assert.NotSame(runner.Actions, revertRunner.Actions);
        Assert.Same(runner.Actions[0], revertRunner.Actions[0]);
    }

    // ===== AutomationPlan =====

    [Fact]
    public void AutomationPlan_Triggered_RuleSetSatisfied_ReturnsRunner()
    {
        var action = new TestAction();
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(true));
        var plan = CreateTestPlan(ruleSet: ruleSet, actions: [action]);

        var runner = plan.Triggered();

        Assert.NotNull(runner);
    }

    [Fact]
    public void AutomationPlan_Triggered_RuleSetNotSatisfied_ReturnsNull()
    {
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(false));
        var plan = CreateTestPlan(ruleSet: ruleSet);

        var runner = plan.Triggered();

        Assert.Null(runner);
    }

    [Fact]
    public void AutomationPlan_Triggered_NullRuleSet_ReturnsRunner()
    {
        var plan = CreateTestPlan(ruleSet: null);

        var runner = plan.Triggered();

        Assert.NotNull(runner);
    }

    [Fact]
    public async Task AutomationPlan_Reverted_NotRevertable_ReturnsNull()
    {
        var action = new TestAction();
        var plan = CreateTestPlan(revertable: false, actions: [action]);
        var originalRunner = new AutomationRunner(Guid.NewGuid(), false, [action]);

        var result = await plan.Reverted(originalRunner);

        Assert.Null(result);
    }

    [Fact]
    public async Task AutomationPlan_Reverted_Revertable_ReturnsRevertRunner()
    {
        var action = new TestAction();
        var plan = CreateTestPlan(revertable: true, actions: [action]);

        var forwardRunner = new AutomationRunner(Guid.NewGuid(), false, [action]);
        await forwardRunner.ExecuteAsync();

        var result = await plan.Reverted(forwardRunner);

        Assert.NotNull(result);
        Assert.True(result!.RevertMode);
    }

    // ===== AutomationPipeline =====

    [Fact]
    public async Task AutomationPipeline_LoadAllPlans_RegistersAllPlans()
    {
        await using var pipeline = new AutomationPipeline();
        var plan1 = CreateTestPlan(guid: Guid.NewGuid());
        var plan2 = CreateTestPlan(guid: Guid.NewGuid());

        await pipeline.LoadAllPlans([plan1, plan2]);
    }

    [Fact]
    public async Task AutomationPipeline_UnloadAllPlans_ClearsAllPlans()
    {
        var pipeline = new AutomationPipeline();
        var plan = CreateTestPlan();

        await pipeline.LoadAllPlans([plan]);
        await pipeline.UnloadAllPlans();
    }

    [Fact]
    public async Task AutomationPipeline_TriggerFires_PlanTriggeredEventRaised()
    {
        await using var pipeline = new AutomationPipeline();
        var trigger = new TestTrigger();
        var action = new TestAction();
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(true));
        var plan = CreateTestPlan(trigger: trigger, ruleSet: ruleSet, actions: [action]);

        await pipeline.LoadAllPlans([plan]);

        PlanTriggeredEventArgs? eventArgs = null;
        pipeline.PlanTriggered += (_, args) => eventArgs = args;

        trigger.Fire(new TriggerFiredEventArgs
        {
            AutomationPlanGuid = plan.Guid,
            FiredAt = DateTime.UtcNow
        });

        await Task.Delay(100);

        Assert.NotNull(eventArgs);
        Assert.Same(plan, eventArgs!.Plan);
    }

    [Fact]
    public async Task AutomationPipeline_TriggerFires_RuleNotSatisfied_NoEventRaised()
    {
        await using var pipeline = new AutomationPipeline();
        var trigger = new TestTrigger();
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(false));
        var plan = CreateTestPlan(trigger: trigger, ruleSet: ruleSet);

        await pipeline.LoadAllPlans([plan]);

        var eventRaised = false;
        pipeline.PlanTriggered += (_, _) => eventRaised = true;

        trigger.Fire(new TriggerFiredEventArgs
        {
            AutomationPlanGuid = plan.Guid,
            FiredAt = DateTime.UtcNow
        });

        await Task.Delay(100);

        Assert.False(eventRaised);
    }

    [Fact]
    public async Task AutomationPipeline_UnregisterPlan_StopsTriggerSubscriptions()
    {
        await using var pipeline = new AutomationPipeline();
        var trigger = new TestTrigger();
        var plan = CreateTestPlan(trigger: trigger);

        await pipeline.LoadAllPlans([plan]);
        pipeline.UnregisterPlan(plan.Guid);

        var eventRaised = false;
        pipeline.PlanTriggered += (_, _) => eventRaised = true;

        trigger.Fire(new TriggerFiredEventArgs
        {
            AutomationPlanGuid = plan.Guid,
            FiredAt = DateTime.UtcNow
        });

        await Task.Delay(100);

        Assert.False(eventRaised);
    }

    [Fact]
    public async Task AutomationPipeline_DisposeAsync_CleansUpAllResources()
    {
        var pipeline = new AutomationPipeline();
        var trigger = new TestTrigger();
        var plan = CreateTestPlan(trigger: trigger);

        await pipeline.LoadAllPlans([plan]);
        await pipeline.DisposeAsync();

        var eventRaised = false;
        pipeline.PlanTriggered += (_, _) => eventRaised = true;

        trigger.Fire(new TriggerFiredEventArgs
        {
            AutomationPlanGuid = plan.Guid,
            FiredAt = DateTime.UtcNow
        });

        await Task.Delay(100);

        Assert.False(eventRaised);
    }

    // ===== TriggerFiredEventArgs =====

    [Fact]
    public void TriggerFiredEventArgs_PropertiesSetCorrectly()
    {
        var planGuid = Guid.NewGuid();
        var firedAt = DateTime.UtcNow;
        var payload = new { Key = "value" };

        var args = new TriggerFiredEventArgs
        {
            AutomationPlanGuid = planGuid,
            FiredAt = firedAt,
            Payload = payload
        };

        Assert.Equal(planGuid, args.AutomationPlanGuid);
        Assert.Equal(firedAt, args.FiredAt);
        Assert.Same(payload, args.Payload);
    }

    [Fact]
    public void TriggerFiredEventArgs_NullPayload_IsAllowed()
    {
        var args = new TriggerFiredEventArgs
        {
            AutomationPlanGuid = Guid.NewGuid(),
            FiredAt = DateTime.UtcNow
        };

        Assert.Null(args.Payload);
    }

    // ===== RevertFailedException =====

    [Fact]
    public void RevertFailedException_PropertiesSetCorrectly()
    {
        var action = new TestAction();
        var innerException = new InvalidOperationException("inner");
        var step = 2;

        var exception = new RevertFailedException(action, step, innerException);

        Assert.Same(action, exception.FailedAction);
        Assert.Equal(step, exception.StepIndex);
        Assert.Same(innerException, exception.InnerException);
        Assert.Contains("step 2", exception.Message);
    }

    // ===== Integration: Plan -> Pipeline -> Runner =====

    [Fact]
    public async Task Integration_PipelineTriggerExecution_ForwardAndRevert()
    {
        await using var pipeline = new AutomationPipeline();
        var trigger = new TestTrigger();
        var action1 = new TestAction();
        var action2 = new TestAction();
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(true));
        var plan = CreateTestPlan(
            trigger: trigger,
            ruleSet: ruleSet,
            actions: [action1, action2],
            revertable: true);

        await pipeline.LoadAllPlans([plan]);

        AutomationRunner? triggeredRunner = null;
        pipeline.PlanTriggered += (_, args) => triggeredRunner = args.Runner;

        trigger.Fire(new TriggerFiredEventArgs
        {
            AutomationPlanGuid = plan.Guid,
            FiredAt = DateTime.UtcNow
        });

        await Task.Delay(200);

        Assert.NotNull(triggeredRunner);
        Assert.True(action1.InvokeCalled);
        Assert.True(action2.InvokeCalled);

        var revertRunner = await plan.Reverted(triggeredRunner!);
        Assert.NotNull(revertRunner);
        Assert.True(action1.RevertCalled);
        Assert.True(action2.RevertCalled);
    }

    [Fact]
    public async Task Integration_MultipleTriggers_MultipleEvents()
    {
        await using var pipeline = new AutomationPipeline();
        var trigger = new TestTrigger();
        var action = new TestAction();
        var plan = CreateTestPlan(trigger: trigger, actions: [action]);

        await pipeline.LoadAllPlans([plan]);

        var triggerCount = 0;
        pipeline.PlanTriggered += (_, _) => triggerCount++;

        trigger.Fire(new TriggerFiredEventArgs
        {
            AutomationPlanGuid = plan.Guid,
            FiredAt = DateTime.UtcNow
        });
        trigger.Fire(new TriggerFiredEventArgs
        {
            AutomationPlanGuid = plan.Guid,
            FiredAt = DateTime.UtcNow
        });

        await Task.Delay(200);

        Assert.Equal(2, triggerCount);
    }

    [Fact]
    public async Task Integration_ActionException_PlanStillReturnsRunner()
    {
        await using var pipeline = new AutomationPipeline();
        var trigger = new TestTrigger();
        var action = new TestAction(onInvoke: () => throw new InvalidOperationException("fail"));
        var ruleSet = CreateRuleSet(RuleSetSatisfyMode.AllSatisfied, new TestRule(true));
        var plan = CreateTestPlan(trigger: trigger, ruleSet: ruleSet, actions: [action]);

        await pipeline.LoadAllPlans([plan]);

        PlanTriggeredEventArgs? eventArgs = null;
        pipeline.PlanTriggered += (_, args) => eventArgs = args;

        trigger.Fire(new TriggerFiredEventArgs
        {
            AutomationPlanGuid = plan.Guid,
            FiredAt = DateTime.UtcNow
        });

        await Task.Delay(200);

        Assert.NotNull(eventArgs);
        await Assert.ThrowsAsync<InvalidOperationException>(() => eventArgs!.Runner.ExecuteAsync());
    }
}
