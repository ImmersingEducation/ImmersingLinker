using ImmersingLinker.Core.Abstractions.Automation;
using Action = ImmersingLinker.Core.Abstractions.Automation.Action;

namespace ImmersingLinker.Core.Models.Automation;

public class AutomationPlan
{
    public Guid Guid { get; init; }
    public string Name { get; set; }
    public bool Revertable { get; set; }
    
    public Trigger Trigger { get; set; }
    public RuleSet RuleSet { get; set; }
    public List<Action> Actions { get; set; }

    public Task Loaded()
    {
        /*
         * 如果 Trigger 继承了 IQueryNecessaryTrigger
         * 就将其推进轮询流水线
         */
        throw new NotImplementedException();
    }

    public Task Unloaded()
    {
        /*
         * 同 Loaded，将 Trigger 推出轮询流水线
         */
        throw new NotImplementedException();
    }

    public Task Triggered()
    {
        /*
         * 开始从上到下执行 Actions 里的行动
         * 并记录当前执行到的行动
         */
        throw new NotImplementedException();
    }

    public Task Reverted()
    {
        /*
         * 等待当前行动执行完毕，从当前行动开始
         * 逆序回滚所有已执行的行动
         */
        throw new NotImplementedException();
    }
}