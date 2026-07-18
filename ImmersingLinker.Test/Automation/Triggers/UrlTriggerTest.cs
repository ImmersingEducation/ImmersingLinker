using ImmersingLinker.Core.Models.Automation;
using ImmersingLinker.Core.Models.Automation.Triggers;

namespace ImmersingLinker.Test.Automation.Triggers;

public class UrlTriggerTest
{
    [Fact]
    public void OnUrlVisited_MatchingTag_FiresTriggerFired()
    {
        var trigger = new UrlTrigger("test-tag");
        var fired = false;
        TriggerFiredEventArgs? receivedArgs = null;
        trigger.TriggerFired += (_, args) =>
        {
            fired = true;
            receivedArgs = args;
        };

        UrlTrigger.OnUrlVisited(null, new TriggerFiredEventArgs
        {
            AutomationPlanGuid = Guid.NewGuid(),
            FiredAt = DateTime.UtcNow,
            Payload = "test-tag"
        });

        Assert.True(fired);
        Assert.NotNull(receivedArgs);
        Assert.Equal("test-tag", receivedArgs!.Payload);
    }

    [Fact]
    public void OnUrlVisited_NonMatchingTag_DoesNotFire()
    {
        var trigger = new UrlTrigger("expected-tag");
        var fired = false;
        trigger.TriggerFired += (_, _) => fired = true;

        UrlTrigger.OnUrlVisited(null, new TriggerFiredEventArgs
        {
            AutomationPlanGuid = Guid.NewGuid(),
            FiredAt = DateTime.UtcNow,
            Payload = "wrong-tag"
        });

        Assert.False(fired);
    }

    [Fact]
    public void OnUrlVisited_NonStringPayload_DoesNotFire()
    {
        var trigger = new UrlTrigger("test-tag");
        var fired = false;
        trigger.TriggerFired += (_, _) => fired = true;

        UrlTrigger.OnUrlVisited(null, new TriggerFiredEventArgs
        {
            AutomationPlanGuid = Guid.NewGuid(),
            FiredAt = DateTime.UtcNow,
            Payload = 123
        });

        Assert.False(fired);
    }

    [Fact]
    public void OnUrlVisited_NullPayload_DoesNotFire()
    {
        var trigger = new UrlTrigger("test-tag");
        var fired = false;
        trigger.TriggerFired += (_, _) => fired = true;

        UrlTrigger.OnUrlVisited(null, new TriggerFiredEventArgs
        {
            AutomationPlanGuid = Guid.NewGuid(),
            FiredAt = DateTime.UtcNow
        });

        Assert.False(fired);
    }

    [Fact]
    public void OnUrlVisited_MultipleInstances_OnlyMatchingFires()
    {
        var triggerA = new UrlTrigger("tag-a");
        var triggerB = new UrlTrigger("tag-b");
        var firedA = false;
        var firedB = false;
        triggerA.TriggerFired += (_, _) => firedA = true;
        triggerB.TriggerFired += (_, _) => firedB = true;

        UrlTrigger.OnUrlVisited(null, new TriggerFiredEventArgs
        {
            AutomationPlanGuid = Guid.NewGuid(),
            FiredAt = DateTime.UtcNow,
            Payload = "tag-a"
        });

        Assert.True(firedA);
        Assert.False(firedB);
    }

    [Fact]
    public void OnUrlVisited_MultipleInstances_BothCanFire()
    {
        var triggerA = new UrlTrigger("tag-a");
        var triggerB = new UrlTrigger("tag-b");
        var firedA = false;
        var firedB = false;
        triggerA.TriggerFired += (_, _) => firedA = true;
        triggerB.TriggerFired += (_, _) => firedB = true;

        UrlTrigger.OnUrlVisited(null, new TriggerFiredEventArgs
        {
            AutomationPlanGuid = Guid.NewGuid(),
            FiredAt = DateTime.UtcNow,
            Payload = "tag-a"
        });
        UrlTrigger.OnUrlVisited(null, new TriggerFiredEventArgs
        {
            AutomationPlanGuid = Guid.NewGuid(),
            FiredAt = DateTime.UtcNow,
            Payload = "tag-b"
        });

        Assert.True(firedA);
        Assert.True(firedB);
    }

    [Fact]
    public void OnUrlVisited_ForwardsSenderToTriggerFired()
    {
        var trigger = new UrlTrigger("test-tag");
        object? receivedSender = null;
        trigger.TriggerFired += (sender, _) => receivedSender = sender;

        var sender = new object();
        UrlTrigger.OnUrlVisited(sender, new TriggerFiredEventArgs
        {
            AutomationPlanGuid = Guid.NewGuid(),
            FiredAt = DateTime.UtcNow,
            Payload = "test-tag"
        });

        Assert.NotNull(receivedSender);
        Assert.NotSame(sender, receivedSender);
        Assert.IsType<UrlTrigger>(receivedSender);
    }
}
