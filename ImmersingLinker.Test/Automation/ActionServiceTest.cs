using System.Reflection;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Attributes.Automation;
using ImmersingLinker.Core.Services.Automation;
using Action = ImmersingLinker.Core.Abstractions.Automation.Action;

namespace ImmersingLinker.Test.Automation;

public class ActionServiceTest
{
    // ===== RegisterAction =====

    [Fact]
    public void RegisterAction_WithAttribute_StoresTypeByKey()
    {
        var service = new ActionService();

        service.RegisterAction(typeof(StubActionA));

        var result = service.GetAction("test.StubActionA");
        Assert.Equal(typeof(StubActionA), result);
    }

    [Fact]
    public void RegisterAction_WithoutAttribute_DoesNotStore()
    {
        var service = new ActionService();

        service.RegisterAction(typeof(NoAttributeAction));

        var result = service.GetAction("no-attr");
        Assert.Null(result);
    }

    [Fact]
    public void RegisterAction_DuplicateKey_OverwritesPrevious()
    {
        var service = new ActionService();
        service.RegisterAction(typeof(StubActionA));

        service.RegisterAction(typeof(StubActionDuplicate));

        var result = service.GetAction("test.StubActionA");
        Assert.Equal(typeof(StubActionDuplicate), result);
    }

    // ===== GetAction =====

    [Fact]
    public void GetAction_ExistingKey_ReturnsType()
    {
        var service = new ActionService();
        service.RegisterAction(typeof(StubActionA));

        var result = service.GetAction("test.StubActionA");

        Assert.Equal(typeof(StubActionA), result);
    }

    [Fact]
    public void GetAction_NonExistingKey_ReturnsNull()
    {
        var service = new ActionService();

        var result = service.GetAction("non-existing");

        Assert.Null(result);
    }

    // ===== UnregisterAction =====

    [Fact]
    public void UnregisterAction_ExistingKey_RemovesAndReturnsTrue()
    {
        var service = new ActionService();
        service.RegisterAction(typeof(StubActionA));

        var result = service.UnregisterAction("test.StubActionA");

        Assert.True(result);
        Assert.Null(service.GetAction("test.StubActionA"));
    }

    [Fact]
    public void UnregisterAction_NonExistingKey_ReturnsFalse()
    {
        var service = new ActionService();

        var result = service.UnregisterAction("non-existing");

        Assert.False(result);
    }

    // ===== ScanAssembly =====

    [Fact]
    public void ScanAssembly_WithActionTypes_RegistersAll()
    {
        var service = new ActionService();

        service.ScanAssembly(typeof(ActionServiceTest).Assembly);

        Assert.Equal(typeof(StubActionDuplicate), service.GetAction("test.StubActionA"));
        Assert.Equal(typeof(StubActionB), service.GetAction("test.StubActionB"));
    }

    [Fact]
    public void ScanAssembly_DoesNotRegisterAbstractTypes()
    {
        var service = new ActionService();

        service.ScanAssembly(typeof(ActionServiceTest).Assembly);

        Assert.Null(service.GetAction("test.AbstractAction"));
    }

    [Fact]
    public void ScanAssembly_DoesNotRegisterWithoutAttribute()
    {
        var service = new ActionService();

        service.ScanAssembly(typeof(ActionServiceTest).Assembly);

        Assert.Null(service.GetAction("no-attr"));
    }

    // ===== Stub types =====

    [Action("test.StubActionA", "Stub A")]
    private class StubActionA : Action
    {
        public override bool Revertable => false;
        public override Task OnInvoke() => Task.CompletedTask;
        public override Task OnRevert() => Task.CompletedTask;
    }

    [Action("test.StubActionB", "Stub B")]
    private class StubActionB : Action
    {
        public override bool Revertable => true;
        public override Task OnInvoke() => Task.CompletedTask;
        public override Task OnRevert() => Task.CompletedTask;
    }

    [Action("test.StubActionA", "Duplicate A")]
    private class StubActionDuplicate : Action
    {
        public override bool Revertable => false;
        public override Task OnInvoke() => Task.CompletedTask;
        public override Task OnRevert() => Task.CompletedTask;
    }

    private abstract class AbstractAction : Action
    {
        public override bool Revertable => false;
        public override Task OnInvoke() => Task.CompletedTask;
        public override Task OnRevert() => Task.CompletedTask;
    }

    private class NoAttributeAction : Action
    {
        public override bool Revertable => false;
        public override Task OnInvoke() => Task.CompletedTask;
        public override Task OnRevert() => Task.CompletedTask;
    }
}
