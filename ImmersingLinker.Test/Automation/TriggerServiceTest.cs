using System.Reflection;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Attributes.Automation;
using ImmersingLinker.Core.Models.Automation.Triggers;
using ImmersingLinker.Core.Services.Automation;

namespace ImmersingLinker.Test.Automation;

public class TriggerServiceTest
{
    // ===== RegisterTrigger =====

    [Fact]
    public void RegisterTrigger_WithAttribute_StoresTypeByKey()
    {
        var service = new TriggerService();

        service.RegisterTrigger(typeof(StubTriggerA));

        var result = service.GetTrigger("test.StubTriggerA");
        Assert.Equal(typeof(StubTriggerA), result);
    }

    [Fact]
    public void RegisterTrigger_WithoutAttribute_DoesNotStore()
    {
        var service = new TriggerService();

        service.RegisterTrigger(typeof(NoAttributeTrigger));

        var result = service.GetTrigger("no-attr");
        Assert.Null(result);
    }

    [Fact]
    public void RegisterTrigger_DuplicateKey_OverwritesPrevious()
    {
        var service = new TriggerService();
        service.RegisterTrigger(typeof(StubTriggerA));

        service.RegisterTrigger(typeof(StubTriggerDuplicate));

        var result = service.GetTrigger("test.StubTriggerA");
        Assert.Equal(typeof(StubTriggerDuplicate), result);
    }

    // ===== GetTrigger =====

    [Fact]
    public void GetTrigger_ExistingKey_ReturnsType()
    {
        var service = new TriggerService();
        service.RegisterTrigger(typeof(StubTriggerA));

        var result = service.GetTrigger("test.StubTriggerA");

        Assert.Equal(typeof(StubTriggerA), result);
    }

    [Fact]
    public void GetTrigger_NonExistingKey_ReturnsNull()
    {
        var service = new TriggerService();

        var result = service.GetTrigger("non-existing");

        Assert.Null(result);
    }

    // ===== UnregisterTrigger =====

    [Fact]
    public void UnregisterTrigger_ExistingKey_RemovesAndReturnsTrue()
    {
        var service = new TriggerService();
        service.RegisterTrigger(typeof(StubTriggerA));

        var result = service.UnregisterTrigger("test.StubTriggerA");

        Assert.True(result);
        Assert.Null(service.GetTrigger("test.StubTriggerA"));
    }

    [Fact]
    public void UnregisterTrigger_NonExistingKey_ReturnsFalse()
    {
        var service = new TriggerService();

        var result = service.UnregisterTrigger("non-existing");

        Assert.False(result);
    }

    // ===== ScanAssembly =====

    [Fact]
    public void ScanAssembly_WithTriggerTypes_RegistersAll()
    {
        var service = new TriggerService();

        service.ScanAssembly(typeof(TriggerServiceTest).Assembly);
        service.ScanAssembly(typeof(Trigger).Assembly);

        Assert.Equal(typeof(StubTriggerDuplicate), service.GetTrigger("test.StubTriggerA"));
        Assert.Equal(typeof(StubTriggerB), service.GetTrigger("test.StubTriggerB"));
        Assert.Equal(typeof(UrlTrigger), service.GetTrigger("ilinker.UrlTrigger"));
    }

    [Fact]
    public void ScanAssembly_DoesNotRegisterAbstractTypes()
    {
        var service = new TriggerService();

        service.ScanAssembly(typeof(TriggerServiceTest).Assembly);

        Assert.Null(service.GetTrigger("test.AbstractTrigger"));
    }

    [Fact]
    public void ScanAssembly_DoesNotRegisterWithoutAttribute()
    {
        var service = new TriggerService();

        service.ScanAssembly(typeof(TriggerServiceTest).Assembly);

        Assert.Null(service.GetTrigger("no-attr"));
    }

    // ===== Stub types =====

    [Trigger("test.StubTriggerA", "Stub A")]
    private class StubTriggerA : Trigger;

    [Trigger("test.StubTriggerB", "Stub B")]
    private class StubTriggerB : Trigger;

    [Trigger("test.StubTriggerA", "Duplicate A")]
    private class StubTriggerDuplicate : Trigger;

    private abstract class AbstractTrigger : Trigger;

    private class NoAttributeTrigger : Trigger;
}
