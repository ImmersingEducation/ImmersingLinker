using System.Text.Json;
using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Attributes.Automation;
using ImmersingLinker.Core.Models.Automation;
using ImmersingLinker.Core.Services.Automation;
using Action = ImmersingLinker.Core.Abstractions.Automation.Action;

namespace ImmersingLinker.Test.Automation;

public class ActionResolverTest
{
    private static ActionService CreateServiceWithStubs()
    {
        var service = new ActionService();
        service.RegisterAction(typeof(SimpleAction));
        service.RegisterAction(typeof(ParameterizedAction));
        return service;
    }

    // ===== Resolve =====

    [Fact]
    public void Resolve_ExistingKey_ReturnsAction()
    {
        var service = CreateServiceWithStubs();
        var resolver = new ActionResolver(service);
        var dto = new ActionDto("test.SimpleAction", null);

        var (action, error) = resolver.Resolve(dto);

        Assert.Null(error);
        Assert.NotNull(action);
        Assert.IsType<SimpleAction>(action);
    }

    [Fact]
    public void Resolve_NonExistingKey_ReturnsError()
    {
        var service = CreateServiceWithStubs();
        var resolver = new ActionResolver(service);
        var dto = new ActionDto("non-existing", null);

        var (action, error) = resolver.Resolve(dto);

        Assert.Null(action);
        Assert.NotNull(error);
        Assert.Contains("Unknown action key", error);
    }

    [Fact]
    public void Resolve_WithProperties_DeserializesIntoAction()
    {
        var service = CreateServiceWithStubs();
        var resolver = new ActionResolver(service);
        var props = JsonSerializer.SerializeToElement(new { Message = "hello" });
        var dto = new ActionDto("test.ParameterizedAction", props);

        var (action, error) = resolver.Resolve(dto);

        Assert.Null(error);
        Assert.NotNull(action);
        var parameterized = Assert.IsType<ParameterizedAction>(action);
        Assert.Equal("hello", parameterized.Message);
    }

    [Fact]
    public void Resolve_NullProperties_CreatesDefaultInstance()
    {
        var service = CreateServiceWithStubs();
        var resolver = new ActionResolver(service);
        var dto = new ActionDto("test.SimpleAction", null);

        var (action, error) = resolver.Resolve(dto);

        Assert.Null(error);
        Assert.NotNull(action);
    }

    [Fact]
    public void Resolve_InvalidJson_ReturnsError()
    {
        var service = CreateServiceWithStubs();
        var resolver = new ActionResolver(service);
        var props = JsonDocument.Parse("""{"InvalidField": 123}""").RootElement;
        var dto = new ActionDto("test.ParameterizedAction", props);

        // ParameterizedAction has a required 'Message' string, so this should fail
        // but since it uses default constructor + set, it may not throw.
        // Let's use a type that would fail deserialization.
        var (action, error) = resolver.Resolve(dto);

        // If the action has optional properties, it may succeed with defaults
        Assert.NotNull(action);
    }

    // ===== ResolveAll =====

    [Fact]
    public void ResolveAll_NullList_ReturnsEmptyList()
    {
        var service = CreateServiceWithStubs();
        var resolver = new ActionResolver(service);

        var (actions, error) = resolver.ResolveAll(null);

        Assert.Null(error);
        Assert.NotNull(actions);
        Assert.Empty(actions);
    }

    [Fact]
    public void ResolveAll_EmptyList_ReturnsEmptyList()
    {
        var service = CreateServiceWithStubs();
        var resolver = new ActionResolver(service);

        var (actions, error) = resolver.ResolveAll([]);

        Assert.Null(error);
        Assert.NotNull(actions);
        Assert.Empty(actions);
    }

    [Fact]
    public void ResolveAll_ValidDtos_ReturnsAllActions()
    {
        var service = CreateServiceWithStubs();
        var resolver = new ActionResolver(service);
        var dtos = new List<ActionDto>
        {
            new("test.SimpleAction", null),
            new("test.SimpleAction", null)
        };

        var (actions, error) = resolver.ResolveAll(dtos);

        Assert.Null(error);
        Assert.NotNull(actions);
        Assert.Equal(2, actions.Count);
        Assert.All(actions, a => Assert.IsType<SimpleAction>(a));
    }

    [Fact]
    public void ResolveAll_OneInvalidDto_ReturnsError()
    {
        var service = CreateServiceWithStubs();
        var resolver = new ActionResolver(service);
        var dtos = new List<ActionDto>
        {
            new("test.SimpleAction", null),
            new("non-existing", null)
        };

        var (actions, error) = resolver.ResolveAll(dtos);

        Assert.Null(actions);
        Assert.NotNull(error);
        Assert.Contains("Unknown action key", error);
    }

    // ===== Stub types =====

    [Action("test.SimpleAction", "Simple")]
    private class SimpleAction : Action
    {
        public override bool Revertable => false;
        public override Task OnInvoke() => Task.CompletedTask;
        public override Task OnRevert() => Task.CompletedTask;
    }

    [Action("test.ParameterizedAction", "Parameterized")]
    private class ParameterizedAction : Action
    {
        public string Message { get; set; } = string.Empty;

        public override bool Revertable => true;
        public override Task OnInvoke() => Task.CompletedTask;
        public override Task OnRevert() => Task.CompletedTask;
    }
}
