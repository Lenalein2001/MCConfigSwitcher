using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Newtonsoft.Json.Linq;
using MCConfigSwitcher.Models;

namespace MCConfigSwitcher.Services;

public class ApplyResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string> FileDiffs { get; set; } = new();
}

public class ConfigApplyService
{
    public ApplyResult DryRun(Profile profile)
    {
        var diffBuilder = InlineDiffBuilder.Instance;
        var result = new ApplyResult { Success = true };

        foreach (var target in profile.Targets)
        {
            if (!File.Exists(target.Path))
            {
                if (!target.CreateIfMissing)
                {
                    result.FileDiffs[target.Path] = "(missing)";
                    continue;
                }
            }
            var original = File.Exists(target.Path) ? File.ReadAllText(target.Path) : string.Empty;
            var modified = ApplyRulesToContent(original, target, profile.Variables, dryRun:true);
            if (original != modified)
            {
                var diff = diffBuilder.BuildDiffModel(original, modified);
                result.FileDiffs[target.Path] = RenderDiff(diff);
            }
            else
            {
                result.FileDiffs[target.Path] = "(no changes)";
            }
        }

        return result;
    }

    public ApplyResult Apply(Profile profile)
    {
        var dry = DryRun(profile);
        if (!dry.Success) return dry;
        foreach (var kv in dry.FileDiffs.Where(d => d.Value != "(no changes)" && d.Value != "(missing)"))
        {
            var target = profile.Targets.First(t => t.Path == kv.Key);
            var original = File.Exists(target.Path) ? File.ReadAllText(target.Path) : string.Empty;
            var modified = ApplyRulesToContent(original, target, profile.Variables, dryRun:false);
            try
            {
                if (target.MakeBackup && File.Exists(target.Path))
                {
                    var backup = target.Path + ".bak_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    File.Copy(target.Path, backup, overwrite:true);
                }
                var dir = Path.GetDirectoryName(target.Path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir!);
                File.WriteAllText(target.Path, modified, GetEncoding(target.EncodingName));
            }
            catch (Exception ex)
            {
                dry.Success = false;
                dry.FileDiffs[target.Path] = "error: " + ex.Message;
            }
        }
        return new ApplyResult { Success = dry.Success, Message = dry.Success ? "Applied." : "Applied with errors.", FileDiffs = dry.FileDiffs};
    }

    // Revert: restore latest backup (lexicographically last) per target.
    public ApplyResult Revert(Profile profile)
    {
        var result = new ApplyResult { Success = true };
        foreach (var target in profile.Targets)
        {
            if (!target.MakeBackup) { result.FileDiffs[target.Path] = "(no backup configured)"; continue; }
            var dir = Path.GetDirectoryName(target.Path);
            if (dir == null || !Directory.Exists(dir)) { result.FileDiffs[target.Path] = "(directory missing)"; continue; }
            var pattern = Path.GetFileName(target.Path) + ".bak_*";
            var backups = Directory.GetFiles(dir, pattern)
                                    .OrderBy(f => f)
                                    .ToList();
            if (backups.Count == 0) { result.FileDiffs[target.Path] = "(no backups found)"; continue; }
            var latest = backups.Last();
            try
            {
                File.Copy(latest, target.Path, overwrite:true);
                result.FileDiffs[target.Path] = "restored from " + Path.GetFileName(latest);
            }
            catch (Exception ex)
            {
                result.FileDiffs[target.Path] = "error: " + ex.Message;
                result.Success = false;
            }
        }
        result.Message = result.Success ? "Revert completed." : "Revert completed with errors.";
        return result;
    }

    private string ApplyRulesToContent(string input, TargetFile target, Dictionary<string,string> vars, bool dryRun)
    {
        var content = input;
        foreach (var rule in target.Rules)
        {
            switch (rule.Kind)
            {
                case RuleKind.LineKey when rule.LineKey != null:
                    content = ReplaceLineKey(content, rule.LineKey, Expand(rule.Value ?? string.Empty, vars));
                    break;
                case RuleKind.Regex when rule.Pattern != null:
                    var repl = Expand(rule.Replacement ?? string.Empty, vars);
                    var options = RegexOptions.Multiline;
                    if (rule.RegexOptions?.Contains("i") == true) options |= RegexOptions.IgnoreCase;
                    content = Regex.Replace(content, rule.Pattern, repl, options);
                    break;
                case RuleKind.JsonPath when rule.JsonPath != null:
                    content = ApplyJsonPath(content, rule.JsonPath, rule.JsonValue); // simple approach; no dry-run difference
                    break;
            }
        }
        return content;
    }

    private string ReplaceLineKey(string content, string key, string value)
    {
        var lines = content.Split('\n');
        var found = false;
        for (int i=0; i<lines.Length; i++)
        {
            var trimmed = lines[i].TrimEnd('\r');
            if (trimmed.StartsWith(key + "="))
            {
                lines[i] = key + "=" + value + (trimmed.EndsWith("\r") ? "\r" : string.Empty);
                found = true;
                break;
            }
        }
        if (!found)
        {
            content += (content.EndsWith("\n") ? string.Empty : "\n") + key + "=" + value + "\n";
            return content;
        }
        return string.Join('\n', lines);
    }

    private string ApplyJsonPath(string content, string path, JToken? value)
    {
        if (value == null) return content;
        JToken token;
        try
        {
            token = string.IsNullOrWhiteSpace(content) ? new JObject() : JToken.Parse(content);
        }
        catch
        {
            token = new JObject();
        }
        if (token is not JObject obj) return content;
        obj[path] = value; // Simplified: treat path as direct property name
        return obj.ToString();
    }

    private string Expand(string raw, Dictionary<string,string> vars)
    {
        var result = raw;
        foreach (var kv in vars)
        {
            result = result.Replace("{" + kv.Key + "}", kv.Value);
        }
        return result;
    }

    private Encoding GetEncoding(string name)
    {
        try { return Encoding.GetEncoding(name); } catch { return Encoding.UTF8; }
    }

    private string RenderDiff(DiffPaneModel diff)
    {
        var sb = new StringBuilder();
        foreach (var line in diff.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Inserted: sb.AppendLine("+ " + line.Text); break;
                case ChangeType.Deleted: sb.AppendLine("- " + line.Text); break;
                case ChangeType.Unchanged: sb.AppendLine("  " + line.Text); break;
                case ChangeType.Modified: sb.AppendLine("~ " + line.Text); break;
            }
        }
        return sb.ToString();
    }
}
