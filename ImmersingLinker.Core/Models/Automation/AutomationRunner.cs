using ImmersingLinker.Core.Exceptions.Automations;

namespace ImmersingLinker.Core.Models.Automation;

using ImmersingLinker.Core.Abstractions.Automation;

public class AutomationRunner(Guid guid, bool revertMode, List<Action> actions)
{
    public Guid Guid { get; init; } = guid;
    public bool RevertMode { get; init; } = revertMode;
    public List<Action> Actions { get; init; } = actions;
    public int CurrentStep { get; private set; } = -1;

    public event EventHandler? Started;
    public event EventHandler? Stopped;
    public event EventHandler<RunnerCompletedEventArgs>? Completed;
    public event EventHandler<RunnerFailedEventArgs>? Failed;

    private readonly List<int> _executedSteps = [];

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        Started?.Invoke(this, EventArgs.Empty);

        try
        {
            if (RevertMode) await ExecuteRevertAsync(ct);
            else await ExecuteForwardAsync(ct);
            Stopped?.Invoke(this, EventArgs.Empty);
            Completed?.Invoke(this, new RunnerCompletedEventArgs { Runner = this });
        }
        catch (Exception ex)
        {
            Stopped?.Invoke(this, EventArgs.Empty);
            Failed?.Invoke(this, new RunnerFailedEventArgs { Runner = this, Exception = ex });
            throw;
        }
    }

    private async Task ExecuteForwardAsync(CancellationToken ct)
    {
        for (CurrentStep = 0; CurrentStep < Actions.Count; CurrentStep++)
        {
            ct.ThrowIfCancellationRequested();
            var action = Actions[CurrentStep];
            await action.OnInvoke();
            _executedSteps.Add(CurrentStep);
        }
    }
    
    private async Task ExecuteRevertAsync(CancellationToken ct)
    {
        for (var i = CurrentStep; i >= 0; i--)
        {
            ct.ThrowIfCancellationRequested();
            var action = Actions[i];
            if (!action.Revertable) continue;

            try
            {
                await action.OnRevert();
            }
            catch (Exception ex)
            {
                await action.OnRevertFailed(ex);
                throw new RevertFailedException(action, i, ex);
            }
        }
    }

    public AutomationRunner CreateRevertRunner()
    {
        return new AutomationRunner(Guid.NewGuid(), true, Actions.ToList())
        {
            CurrentStep = CurrentStep,
        };
    }
}