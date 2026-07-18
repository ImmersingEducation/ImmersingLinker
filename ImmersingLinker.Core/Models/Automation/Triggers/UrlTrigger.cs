using ImmersingLinker.Core.Abstractions.Automation;
using ImmersingLinker.Core.Attributes.Automation;

namespace ImmersingLinker.Core.Models.Automation.Triggers;

[Trigger(key: "ilinker.UrlTrigger", name: "指定 URL 被访问时")]
public class UrlTrigger : Trigger
{
    public static event EventHandler<TriggerFiredEventArgs> UrlVisited;
    
    public string Tag { get; set; }

    public UrlTrigger(string tag)
    {
        Tag = tag;
        UrlVisited += OnTriggerFired;
    }

    protected override void OnTriggerFired(object? sender, TriggerFiredEventArgs? args)
    {
        if (args?.Payload is string url)
        {
            if (url == Tag)
                base.OnTriggerFired(this, args);
        }
    }
}