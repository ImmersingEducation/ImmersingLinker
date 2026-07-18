using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Enums.Automation;
using ImmersingLinker.Core.Models.Automation;
using ImmersingLinker.Core.Models.Automation.Triggers;
using ImmersingLinker.Server.Controllers;
using ImmersingLinker.Core.Services.Storage;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Action = ImmersingLinker.Core.Abstractions.Automation.Action;

namespace ImmersingLinker.Test.Controllers;

public class AutomationControllerTest
{
    private static readonly Guid TestPlanGuid = Guid.NewGuid();
    private const string TestPlanName = "TestPlan";

    // ===== Test Stubs =====

    private class TestTrigger : Trigger, IManualTrigger
    {
        public bool Fired { get; private set; }

        public void Fire(TriggerFiredEventArgs args)
        {
            Fired = true;
            OnTriggerFired(this, args);
        }
    }

    private class TestNonManualTrigger : Trigger
    {
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
        public override bool Revertable => true;

        public override Task OnInvoke() => Task.CompletedTask;
        public override Task OnRevert() => Task.CompletedTask;
    }

    // ===== Helpers =====

    private static AutomationPlan CreateTestPlan(Trigger? trigger = null)
    {
        var ruleSet = new RuleSet
        {
            SatisfyMode = RuleSetSatisfyMode.AllSatisfied
        };
        ruleSet.AddRule(new TestRule(true));

        return new AutomationPlan
        {
            Guid = TestPlanGuid,
            Name = TestPlanName,
            Revertable = true,
            Trigger = trigger ?? new TestTrigger(),
            RuleSet = ruleSet,
            Actions = [new TestAction()]
        };
    }

    private static TriggerDto CreateTriggerDto()
    {
        return new TriggerDto("test.Trigger", null);
    }

    private static Mock<IAutomationStorageService> CreateMockStorage(AutomationPlan? plan = null)
    {
        plan ??= CreateTestPlan();
        var mock = new Mock<IAutomationStorageService>();

        mock.Setup(s => s.GetPlanInfos())
            .ReturnsAsync([new AutomationPlanInfo { Guid = TestPlanGuid, Name = TestPlanName }]);

        mock.Setup(s => s.GetPlan(It.Is<Guid>(g => g == TestPlanGuid)))
            .ReturnsAsync(plan);

        mock.Setup(s => s.GetPlan(It.Is<Guid>(g => g != TestPlanGuid)))
            .ReturnsAsync((AutomationPlan?)null);

        return mock;
    }

    private static Mock<IAutomationPipeline> CreateMockPipeline(AutomationPlan? plan = null)
    {
        plan ??= CreateTestPlan();
        var mock = new Mock<IAutomationPipeline>();

        mock.Setup(p => p.GetRegisteredPlan(It.Is<Guid>(g => g == TestPlanGuid)))
            .Returns(plan);

        mock.Setup(p => p.GetRegisteredPlan(It.Is<Guid>(g => g != TestPlanGuid)))
            .Returns((AutomationPlan?)null);

        return mock;
    }

    private static Mock<ITriggerResolver> CreateMockTriggerResolver(Trigger? trigger = null)
    {
        trigger ??= new TestTrigger();
        var mock = new Mock<ITriggerResolver>();
        mock.Setup(r => r.Resolve(It.IsAny<TriggerDto>()))
            .Returns((trigger, (string?)null));
        return mock;
    }

    private static Mock<IRuleResolver> CreateMockRuleResolver(RuleSet? ruleSet = null)
    {
        var mock = new Mock<IRuleResolver>();
        mock.Setup(r => r.ResolveRuleSet(It.IsAny<RuleSetDto?>()))
            .Returns((ruleSet, (string?)null));
        return mock;
    }

    private static Mock<IActionResolver> CreateMockActionResolver(List<Action>? actions = null)
    {
        actions ??= [new TestAction()];
        var mock = new Mock<IActionResolver>();
        mock.Setup(r => r.ResolveAll(It.IsAny<List<ActionDto>?>()))
            .Returns((actions, (string?)null));
        return mock;
    }

    private static AutomationController CreateController(
        Mock<IAutomationStorageService>? storageMock = null,
        Mock<IAutomationPipeline>? pipelineMock = null,
        Mock<ITriggerResolver>? triggerResolverMock = null,
        Mock<IRuleResolver>? ruleResolverMock = null,
        Mock<IActionResolver>? actionResolverMock = null)
    {
        storageMock ??= CreateMockStorage();
        pipelineMock ??= CreateMockPipeline();
        triggerResolverMock ??= CreateMockTriggerResolver();
        ruleResolverMock ??= CreateMockRuleResolver();
        actionResolverMock ??= CreateMockActionResolver();
        return new AutomationController(
            storageMock.Object,
            pipelineMock.Object,
            triggerResolverMock.Object,
            ruleResolverMock.Object,
            actionResolverMock.Object);
    }

    // ===== GET =====

    [Fact]
    public async Task GetAllPlanInfos_ReturnsInfos()
    {
        var controller = CreateController();
        var result = await controller.GetAllPlanInfos();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var infos = Assert.IsType<List<AutomationPlanInfo>>(okResult.Value);
        Assert.Single(infos);
        Assert.Equal(TestPlanName, infos[0].Name);
    }

    [Fact]
    public async Task GetPlanByGuid_Existing_ReturnsPlan()
    {
        var controller = CreateController();
        var result = await controller.GetPlanByGuid(TestPlanGuid.ToString());

        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<AutomationPlan>(okResult.Value);
        Assert.Equal(TestPlanName, plan.Name);
        Assert.True(plan.Revertable);
    }

    [Fact]
    public async Task GetPlanByGuid_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.GetPlanByGuid(Guid.NewGuid().ToString());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetPlanByGuid_InvalidGuid_ReturnsBadRequest()
    {
        var controller = CreateController();
        var result = await controller.GetPlanByGuid("bad-guid");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ===== POST Create =====

    [Fact]
    public async Task CreatePlan_ReturnsCreated()
    {
        var storageMock = CreateMockStorage();
        var pipelineMock = CreateMockPipeline();
        var controller = CreateController(storageMock, pipelineMock);

        var request = new CreateAutomationPlanRequest(
            "NewPlan", true, CreateTriggerDto(), null, [new ActionDto("test.Action", null)]);

        var result = await controller.CreatePlan(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(AutomationController.GetPlanByGuid), createdResult.ActionName);
        var plan = Assert.IsType<AutomationPlan>(createdResult.Value);
        Assert.Equal("NewPlan", plan.Name);
        Assert.True(plan.Revertable);
        storageMock.Verify(s => s.SavePlan(It.IsAny<AutomationPlan>()), Times.Once);
        pipelineMock.Verify(p => p.RegisterPlan(It.IsAny<AutomationPlan>()), Times.Never);
    }

    [Fact]
    public async Task CreatePlan_TriggerResolutionFailed_ReturnsBadRequest()
    {
        var triggerResolverMock = new Mock<ITriggerResolver>();
        triggerResolverMock.Setup(r => r.Resolve(It.IsAny<TriggerDto>()))
            .Returns(((Trigger?)null, "Unknown trigger key: bad.Key"));

        var controller = CreateController(triggerResolverMock: triggerResolverMock);

        var request = new CreateAutomationPlanRequest(
            "NewPlan", true, new TriggerDto("bad.Key", null), null, []);

        var result = await controller.CreatePlan(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Unknown trigger key", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task CreatePlan_RuleResolutionFailed_ReturnsBadRequest()
    {
        var ruleResolverMock = new Mock<IRuleResolver>();
        ruleResolverMock.Setup(r => r.ResolveRuleSet(It.IsAny<RuleSetDto?>()))
            .Returns(((RuleSet?)null, "Unknown rule key: bad.Rule"));

        var ruleSetDto = new RuleSetDto(RuleSetSatisfyMode.AllSatisfied, false,
        [
            new RuleNodeDto { RuleKey = "bad.Rule" }
        ]);

        var controller = CreateController(ruleResolverMock: ruleResolverMock);

        var request = new CreateAutomationPlanRequest(
            "NewPlan", true, CreateTriggerDto(), ruleSetDto, []);

        var result = await controller.CreatePlan(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Unknown rule key", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task CreatePlan_ActionResolutionFailed_ReturnsBadRequest()
    {
        var actionResolverMock = new Mock<IActionResolver>();
        actionResolverMock.Setup(r => r.ResolveAll(It.IsAny<List<ActionDto>?>()))
            .Returns(((List<Action>?)null, "Unknown action key: bad.Action"));

        var controller = CreateController(actionResolverMock: actionResolverMock);

        var request = new CreateAutomationPlanRequest(
            "NewPlan", true, CreateTriggerDto(), null,
            [new ActionDto("bad.Action", null)]);

        var result = await controller.CreatePlan(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Unknown action key", badRequest.Value!.ToString());
    }

    // ===== POST Trigger =====

    [Fact]
    public async Task TriggerPlan_ManualTrigger_ReturnsOk()
    {
        var trigger = new TestTrigger();
        var plan = CreateTestPlan(trigger);
        var pipelineMock = CreateMockPipeline(plan);
        var controller = CreateController(pipelineMock: pipelineMock);

        var result = await controller.TriggerPlan(TestPlanGuid.ToString());

        Assert.IsType<OkResult>(result);
        Assert.True(trigger.Fired);
    }

    [Fact]
    public async Task TriggerPlan_NotRegistered_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.TriggerPlan(Guid.NewGuid().ToString());

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFoundResult.Value!.ToString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TriggerPlan_NonManualTrigger_ReturnsBadRequest()
    {
        var plan = CreateTestPlan(new TestNonManualTrigger());
        var pipelineMock = CreateMockPipeline(plan);
        var controller = CreateController(pipelineMock: pipelineMock);

        var result = await controller.TriggerPlan(TestPlanGuid.ToString());

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("manual", badRequestResult.Value!.ToString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TriggerPlan_InvalidGuid_ReturnsBadRequest()
    {
        var controller = CreateController();
        var result = await controller.TriggerPlan("bad-guid");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ===== POST Invoke =====

    [Fact]
    public void InvokeUrlTrigger_ReturnsOk()
    {
        var controller = CreateController();

        var result = controller.InvokeUrlTrigger("my-tag");

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public void InvokeUrlTrigger_CallsOnUrlVisitedWithTagAsPayload()
    {
        var controller = CreateController();
        var planGuid = Guid.NewGuid();
        var trigger = new UrlTrigger("my-tag");
        TriggerFiredEventArgs? receivedArgs = null;
        trigger.TriggerFired += (_, args) => receivedArgs = args;

        controller.InvokeUrlTrigger("my-tag");

        Assert.NotNull(receivedArgs);
        Assert.Equal("my-tag", receivedArgs!.Payload);
    }

    [Fact]
    public void InvokeUrlTrigger_NonMatchingTag_DoesNotFireTrigger()
    {
        var controller = CreateController();
        var trigger = new UrlTrigger("expected-tag");
        var fired = false;
        trigger.TriggerFired += (_, _) => fired = true;

        controller.InvokeUrlTrigger("wrong-tag");

        Assert.False(fired);
    }

    // ===== PUT =====

    [Fact]
    public async Task UpdatePlan_Existing_ReturnsOk()
    {
        var storageMock = CreateMockStorage();
        var pipelineMock = CreateMockPipeline();
        var controller = CreateController(storageMock, pipelineMock);

        var request = new UpdateAutomationPlanRequest(
            "UpdatedPlan", false, CreateTriggerDto(), null, [new ActionDto("test.Action", null)]);

        var result = await controller.UpdatePlan(TestPlanGuid.ToString(), request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var plan = Assert.IsType<AutomationPlan>(okResult.Value);
        Assert.Equal("UpdatedPlan", plan.Name);
        Assert.False(plan.Revertable);
        pipelineMock.Verify(p => p.UnregisterPlan(TestPlanGuid), Times.Once);
        storageMock.Verify(s => s.SavePlan(It.IsAny<AutomationPlan>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePlan_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var request = new UpdateAutomationPlanRequest(
            "X", false, CreateTriggerDto(), null, []);

        var result = await controller.UpdatePlan(Guid.NewGuid().ToString(), request);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdatePlan_InvalidGuid_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = new UpdateAutomationPlanRequest(
            "X", false, CreateTriggerDto(), null, []);

        var result = await controller.UpdatePlan("bad-guid", request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ===== DELETE =====

    [Fact]
    public async Task DeletePlan_Existing_ReturnsNoContent()
    {
        var storageMock = CreateMockStorage();
        var pipelineMock = CreateMockPipeline();
        var controller = CreateController(storageMock, pipelineMock);

        var result = await controller.DeletePlan(TestPlanGuid.ToString());

        Assert.IsType<NoContentResult>(result);
        pipelineMock.Verify(p => p.UnregisterPlan(TestPlanGuid), Times.Once);
        storageMock.Verify(s => s.DeletePlan(TestPlanGuid), Times.Once);
    }

    [Fact]
    public async Task DeletePlan_NotFound_ReturnsNotFound()
    {
        var controller = CreateController();
        var result = await controller.DeletePlan(Guid.NewGuid().ToString());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeletePlan_InvalidGuid_ReturnsBadRequest()
    {
        var controller = CreateController();
        var result = await controller.DeletePlan("bad-guid");

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
