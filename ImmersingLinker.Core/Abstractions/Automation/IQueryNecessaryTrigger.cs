namespace ImmersingLinker.Core.Abstractions.Automation;

public interface IQueryNecessaryTrigger
{
    TimeSpan PollingInterval { get; }
    Task<bool> CheckConditionAsync();
}
