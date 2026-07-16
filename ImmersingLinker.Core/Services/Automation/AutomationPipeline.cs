using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Models.Automation;

namespace ImmersingLinker.Core.Services.Automation;

public class AutomationPipeline : IAsyncDisposable
{
    private readonly Dictionary<Guid, AutomationPlan> _plans = [];
    private readonly Dictionary<Guid, List<EventHandler<TriggerFiredEventArgs>>> _subscribers = [];
    private readonly Dictionary<Guid, Timer> _pollingTimers = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _pollingCts = [];

    public event EventHandler<PlanTriggeredEventArgs>? PlanTriggered;
    public event EventHandler<PlanRevertedEventArgs>? PlanReverted;

    public void RegisterPlan(AutomationPlan plan)
    {
        _plans[plan.Guid] = plan;
    }

    public void UnregisterPlan(Guid planGuid)
    {
        _plans.Remove(planGuid);
        UnsubscribeAll(planGuid);
        UnregisterPollingTriggers(planGuid);
    }

    public async Task LoadAllPlans(IEnumerable<AutomationPlan> plans)
    {
        foreach (var plan in plans)
        {
            RegisterPlan(plan);
            await plan.Loaded(this);
        }
    }

    public async Task UnloadAllPlans()
    {
        foreach (var plan in _plans.Values)
        {
            await plan.Unloaded(this);
        }
        _plans.Clear();
    }

    // --- Trigger 订阅管理 ---

    public void SubscribeTrigger(AutomationPlan plan)
    {
        var handler = new EventHandler<TriggerFiredEventArgs>((sender, args) =>
        {
            OnTriggerFired(plan, args);
        });
        plan.Trigger.TriggerFired += handler;
        if (!_subscribers.ContainsKey(plan.Guid))
            _subscribers[plan.Guid] = [];
        _subscribers[plan.Guid].Add(handler);
    }

    public void UnsubscribeTrigger(AutomationPlan plan)
    {
        if (_subscribers.TryGetValue(plan.Guid, out var handlers))
        {
            foreach (var handler in handlers)
            {
                plan.Trigger.TriggerFired -= handler;
            }
            handlers.Clear();
        }
    }

    private void UnsubscribeAll(Guid planGuid)
    {
        if (!_subscribers.TryGetValue(planGuid, out var handlers)) return;

        if (_plans.TryGetValue(planGuid, out var plan))
        {
            foreach (var handler in handlers)
            {
                plan.Trigger.TriggerFired -= handler;
            }
        }
        handlers.Clear();
    }

    private void OnTriggerFired(AutomationPlan plan, TriggerFiredEventArgs args)
    {
        var runner = plan.Triggered();
        if (runner is null) return;
        PlanTriggered?.Invoke(this, new PlanTriggeredEventArgs
        {
            Plan = plan,
            Runner = runner
        });
    }

    // --- 轮询触发器管理 ---

    public Task RegisterPollingTrigger(AutomationPlan plan, IQueryNecessaryTrigger queryNecessaryTrigger)
    {
        var cts = new CancellationTokenSource();
        _pollingCts[plan.Guid] = cts;

        var interval = queryNecessaryTrigger.PollingInterval;

        var timer = new Timer(async _ =>
        {
            if (cts.Token.IsCancellationRequested) return;
            try
            {
                if (await queryNecessaryTrigger.CheckConditionAsync())
                {
                    OnTriggerFired(plan, new TriggerFiredEventArgs
                    {
                        AutomationPlanGuid = plan.Guid,
                        FiredAt = DateTime.UtcNow,
                    });
                }
            }
            catch
            {
            }
        }, null, interval, interval);

        _pollingTimers[plan.Guid] = timer;
        return Task.CompletedTask;
    }

    public Task UnregisterPollingTrigger(Guid planGuid)
    {
        UnregisterPollingTriggers(planGuid);
        return Task.CompletedTask;
    }

    private void UnregisterPollingTriggers(Guid planGuid)
    {
        if (_pollingCts.TryGetValue(planGuid, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _pollingCts.Remove(planGuid);
        }

        if (_pollingTimers.TryGetValue(planGuid, out var timer))
        {
            timer.Dispose();
            _pollingTimers.Remove(planGuid);
        }
    }

    // --- IAsyncDisposable ---

    public async ValueTask DisposeAsync()
    {
        foreach (var plan in _plans.Values)
        {
            await plan.Unloaded(this);
        }

        foreach (var timer in _pollingTimers.Values)
            await timer.DisposeAsync();

        foreach (var cts in _pollingCts.Values)
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        foreach (var handlers in _subscribers.Values)
            handlers.Clear();

        _pollingTimers.Clear();
        _pollingCts.Clear();
        _plans.Clear();
        _subscribers.Clear();
    }
}
