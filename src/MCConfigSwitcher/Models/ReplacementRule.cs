using Newtonsoft.Json.Linq;

namespace MCConfigSwitcher.Models;

public enum RuleKind
{
    LineKey,
    Regex,
    JsonPath
}

public class ReplacementRule
{
    public RuleKind Kind { get; set; }

    // Line-key replacement (e.g., server.properties)
    public string? LineKey { get; set; }
    public string? Value { get; set; }

    // Regex replacement
    public string? Pattern { get; set; }
    public string? Replacement { get; set; }
    public string? RegexOptions { get; set; }

    // JSON path replacement
    public string? JsonPath { get; set; }
    public JToken? JsonValue { get; set; }
}
