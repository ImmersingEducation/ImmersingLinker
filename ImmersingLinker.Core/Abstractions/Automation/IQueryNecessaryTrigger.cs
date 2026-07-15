namespace ImmersingLinker.Core.Abstractions.Automation;

public interface IQueryNecessaryTrigger
{
    public async Task<Timer> StartQuery()
    {
        /*
         * 函数内新建定时器实例，通过该实例创建定时轮询任务
         * 然后将定时器作为返回值返回，由主线程统一管理定时器
         */
        throw new NotImplementedException();
    }

    public async Task StopQuery(Timer timer)
    {
        /*
         * 将该 Trigger 的 StartQuery 创建的定时器作为参数传入
         * 由该函数对定时任务进行停止和资源释放操作
         */
        await timer.DisposeAsync();
    }
}