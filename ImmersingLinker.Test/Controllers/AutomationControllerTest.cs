using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Enums.Automation;
using ImmersingLinker.Core.Models.Automation;
using ImmersingLinker.Server.Controllers;
using ImmersingLinker.Core.Services.Automation;
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
            OnTriggerFired(args);
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

    private static AutomationController CreateController(
        Mock<IAutomationStorageService>? storageMock = null,
        Mock<IAutomationPipeline>? pipelineMock = null)
    {
        storageMock ??= CreateMockStorage();
        pipelineMock ??= CreateMockPipeline();
        return new AutomationController(storageMock.Object, pipelineMock.Object);
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

        var trigger = new TestTrigger();
        var request = new CreateAutomationPlanRequest(
            "NewPlan", true, trigger, null, [new TestAction()]);

        var result = await controller.CreatePlan(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(AutomationController.GetPlanByGuid), createdResult.ActionName);
        var plan = Assert.IsType<AutomationPlan>(createdResult.Value);
        Assert.Equal("NewPlan", plan.Name);
        Assert.True(plan.Revertable);
        storageMock.Verify(s => s.SavePlan(It.IsAny<AutomationPlan>()), Times.Once);
        pipelineMock.Verify(p => p.RegisterPlan(It.IsAny<AutomationPlan>()), Times.Never);
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

    // ===== PUT =====

    [Fact]
    public async Task UpdatePlan_Existing_ReturnsOk()
    {
        var storageMock = CreateMockStorage();
        var pipelineMock = CreateMockPipeline();
        var controller = CreateController(storageMock, pipelineMock);

        var request = new UpdateAutomationPlanRequest(
            "UpdatedPlan", false, new TestTrigger(), null, [new TestAction()]);

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
            "X", false, new TestTrigger(), null, []);

        var result = await controller.UpdatePlan(Guid.NewGuid().ToString(), request);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdatePlan_InvalidGuid_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = new UpdateAutomationPlanRequest(
            "X", false, new TestTrigger(), null, []);

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
