using System.Collections.Generic;

namespace MCConfigSwitcher.Models;

public class MatchRules
{
    public List<string> SsidEquals { get; set; } = new();
    public List<string> SsidContains { get; set; } = new();
    public List<string> HostnameContains { get; set; } = new();
    public List<string> SubnetContains { get; set; } = new();
}
