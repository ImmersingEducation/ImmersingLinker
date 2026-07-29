namespace ImmersingLinker.Core.Models.AccessControl;

public sealed class AccessControlData
{
    public List<AccessControlEntry> Whitelist { get; set; } = [];
    public List<AccessControlEntry> Blacklist { get; set; } = [];
}
