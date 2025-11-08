using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MCConfigSwitcher.Models;
using MCConfigSwitcher.Services;
using System.Text.RegularExpressions;
using System.IO;

namespace MCConfigSwitcher.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ProfileManager _profileManager;
    private readonly ConfigApplyService _applyService;
    private readonly LoggingService _log;

    public ObservableCollection<Profile> Profiles { get; } = new();

    [ObservableProperty]
    private Profile? selectedProfile;

    [ObservableProperty]
    private string currentServerIp = string.Empty;

    [ObservableProperty]
    private bool createBackups = true;

    public MainViewModel()
    {
        _profileManager = new ProfileManager();
        _applyService = new ConfigApplyService();
        _log = new LoggingService();
        _profileManager.Load();
    foreach (var p in _profileManager.Profiles) Profiles.Add(p);
        if (Profiles.Count > 0) SelectedProfile = Profiles[0];
    }

    public ObservableCollection<LogEntry> LogEntries => _log.Entries;

    // Dynamic IP entries: list of name/value pairs stored in Variables as IP_<Name>=value
    public ObservableCollection<(string Name, string Value)> IpEntries { get; } = new();

    public IpEntry? SelectedIp { get; set; }

    [RelayCommand]
    private void AddProfile()
    {
        var p = _profileManager.Create("Profile " + (Profiles.Count + 1));
        Profiles.Add(p);
        SelectedProfile = p;
        _profileManager.Save();
        _log.Info($"Added profile '{p.Name}'.");
    }

    [RelayCommand]
    private void DuplicateProfile()
    {
        if (SelectedProfile == null) return;
        var src = SelectedProfile;
        var copy = new Profile
        {
            Name = src.Name + " (Copy)",
            Variables = new System.Collections.Generic.Dictionary<string, string>(src.Variables),
            Match = new MatchRules
            {
                SsidEquals = src.Match.SsidEquals.ToList(),
                SsidContains = src.Match.SsidContains.ToList(),
                HostnameContains = src.Match.HostnameContains.ToList(),
                SubnetContains = src.Match.SubnetContains.ToList()
            },
            Targets = new System.Collections.ObjectModel.ObservableCollection<TargetFile>(
                src.Targets.Select(t => new TargetFile
                {
                    Path = t.Path,
                    EncodingName = t.EncodingName,
                    MakeBackup = t.MakeBackup,
                    CreateIfMissing = t.CreateIfMissing,
                    Rules = t.Rules.Select(r => new ReplacementRule
                    {
                        Kind = r.Kind,
                        LineKey = r.LineKey,
                        Value = r.Value,
                        Pattern = r.Pattern,
                        Replacement = r.Replacement,
                        RegexOptions = r.RegexOptions,
                        JsonPath = r.JsonPath,
                        JsonValue = r.JsonValue?.DeepClone()
                    }).ToList()
                }))
        };
        _profileManager.Profiles.Add(copy);
        Profiles.Add(copy);
        SelectedProfile = copy;
        _profileManager.Save();
        _log.Info($"Duplicated profile '{src.Name}'.");
    }

    [RelayCommand]
    private void DeleteProfile()
    {
        if (SelectedProfile == null) return;
        var id = SelectedProfile.Id;
        if (_profileManager.Delete(id))
        {
            Profiles.Remove(SelectedProfile);
            SelectedProfile = Profiles.Count > 0 ? Profiles[0] : null;
            _profileManager.Save();
            _log.Warn($"Deleted profile '{id}'.");
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private void DryRun()
    {
        if (SelectedProfile == null) return;
        EnsureDefaultTargetAndRule();
        MapServerIp();
        var r = _applyService.DryRun(SelectedProfile);
        foreach (var kv in r.FileDiffs) _log.Info($"DryRun {kv.Key}: {(kv.Value == "(no changes)" || kv.Value == "(missing)" ? kv.Value : kv.Value.Split('\n').Length + " lines diff")}");
        ReadCurrentServerIpFromFile();
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void Apply()
    {
        if (SelectedProfile == null) return;
        EnsureDefaultTargetAndRule();
        MapServerIp();
        
        // Update backup setting for all targets
        foreach (var target in SelectedProfile.Targets)
        {
            target.MakeBackup = CreateBackups;
        }
        
        var r = _applyService.Apply(SelectedProfile);
        _profileManager.Save();
        _log.Info(r.Message);
        ReadCurrentServerIpFromFile();
    }

    [RelayCommand]
    private void Revert()
    {
        if (SelectedProfile == null) return;
        var r = _applyService.Revert(SelectedProfile);
        _log.Warn(r.Message);
        ReadCurrentServerIpFromFile();
    }

    [RelayCommand]
    private void BrowseFile()
    {
        if (SelectedProfile == null) return;
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select server.properties or target config",
            Filter = "All files|*.*|Properties|*.properties|JSON|*.json",
            CheckFileExists = false
        };
        if (dlg.ShowDialog() == true)
        {
            EnsureDefaultTargetAndRule();
            SelectedProfile.Targets[0].Path = dlg.FileName; // Observable change will update binding now
            SaveProfiles();
            _log.Info($"Target file set to {dlg.FileName}");
        }
    }

    partial void OnSelectedProfileChanged(Profile? value)
    {
        EnsureDefaultTargetAndRule();
        LoadIpEntries();
        if (value != null)
        {
            value.PropertyChanged -= Profile_PropertyChanged;
            value.PropertyChanged += Profile_PropertyChanged;
            
            // Wire Targets collection to read file when path changes
            if (value.Targets.Count > 0)
            {
                value.Targets[0].PropertyChanged -= Target_PropertyChanged;
                value.Targets[0].PropertyChanged += Target_PropertyChanged;
            }
        }
        WireIpEntryHandlers();
        MapServerIp();
        ReadCurrentServerIpFromFile();
    }

    private void Target_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetFile.Path))
        {
            ReadCurrentServerIpFromFile();
        }
    }

    private void Profile_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Profile.ActiveIpChoice))
        {
            MapServerIp();
            SaveProfiles();
        }
        else if (e.PropertyName == nameof(Profile.Name))
        {
            SaveProfiles();
        }
    }

    private void ReadCurrentServerIpFromFile()
    {
        if (SelectedProfile == null || SelectedProfile.Targets.Count == 0)
        {
            CurrentServerIp = "(no target file)";
            return;
        }

        var targetPath = SelectedProfile.Targets[0].Path;
        if (string.IsNullOrWhiteSpace(targetPath) || !File.Exists(targetPath))
        {
            CurrentServerIp = "(file not found)";
            return;
        }

        try
        {
            var lines = File.ReadAllLines(targetPath);
            var serverIpLine = lines.FirstOrDefault(l => l.TrimStart().StartsWith("server-ip="));
            
            if (serverIpLine != null)
            {
                var value = serverIpLine.Split('=', 2).LastOrDefault()?.Trim();
                CurrentServerIp = string.IsNullOrWhiteSpace(value) 
                    ? "(empty - server will auto-detect)" 
                    : value;
            }
            else
            {
                CurrentServerIp = "(server-ip not found in file)";
            }
        }
        catch
        {
            CurrentServerIp = "(error reading file)";
        }
    }

    private void MapServerIp()
    {
        if (SelectedProfile == null) return;
        if (SelectedProfile.IpEntries.Any())
        {
            var activeName = SelectedProfile.ActiveIpChoice;
            var ip = SelectedProfile.IpEntries.FirstOrDefault(e => e.Name == activeName) ?? SelectedProfile.IpEntries.First();
            // Allow empty address (for server auto-detect)
            var serverIp = ip.Address ?? string.Empty;
            SelectedProfile.Variables["SERVER_IP"] = serverIp;
        }
    }

    private void LoadIpEntries()
    {
        // No-op when using SelectedProfile.IpEntries directly in bindings.
    }

    [RelayCommand]
    private void AddIp()
    {
        if (SelectedProfile == null) return;
        var baseName = "Location";
        int i = 1;
        string candidate;
        do { candidate = baseName + i++; } while (SelectedProfile.IpEntries.Any(e => e.Name == candidate));
        var entry = new IpEntry { Name = candidate, Address = string.Empty };
        SelectedProfile.IpEntries.Add(entry);
        SelectedProfile.ActiveIpChoice = entry.Name;
        SaveProfiles();
    }

    [RelayCommand]
    private void DeleteIp(IpEntry? entry)
    {
        if (SelectedProfile == null || entry == null) return;
        var wasActive = SelectedProfile.ActiveIpChoice == entry.Name;
        SelectedProfile.IpEntries.Remove(entry);
        if (wasActive)
            SelectedProfile.ActiveIpChoice = SelectedProfile.IpEntries.FirstOrDefault()?.Name ?? string.Empty;
        SaveProfiles();
    }

    [RelayCommand]
    // Helper to wire changes on IP entries to save and map
    private void WireIpEntryHandlers()
    {
        if (SelectedProfile == null) return;
        foreach (var ip in SelectedProfile.IpEntries)
        {
            ip.PropertyChanged -= Ip_PropertyChanged;
            ip.PropertyChanged += Ip_PropertyChanged;
        }
        SelectedProfile.IpEntries.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
                foreach (IpEntry ip in e.NewItems) ip.PropertyChanged += Ip_PropertyChanged;
            if (e.OldItems != null)
                foreach (IpEntry ip in e.OldItems) ip.PropertyChanged -= Ip_PropertyChanged;
        };
    }

    private void Ip_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        MapServerIp();
        SaveProfiles();
        ApplyCommand.NotifyCanExecuteChanged();
        DryRunCommand.NotifyCanExecuteChanged();
    }

    private void EnsureDefaultTargetAndRule()
    {
        if (SelectedProfile == null) return;
        if (SelectedProfile.Targets.Count == 0)
        {
            SelectedProfile.Targets.Add(new TargetFile
            {
                Path = string.Empty,
                EncodingName = "utf-8",
                MakeBackup = true,
                CreateIfMissing = true,
                Rules = { new ReplacementRule { Kind = RuleKind.LineKey, LineKey = "server-ip", Value = "{SERVER_IP}" } }
            });
        }
        else
        {
            var tf = SelectedProfile.Targets[0];
            if (!tf.Rules.Any(r => r.Kind == RuleKind.LineKey && r.LineKey == "server-ip"))
            {
                tf.Rules.Add(new ReplacementRule { Kind = RuleKind.LineKey, LineKey = "server-ip", Value = "{SERVER_IP}" });
            }
        }
    }

    private void SaveProfiles() => _profileManager.Save();

    // Validation helpers
    private static readonly Regex IPv4Regex = new(
        @"^(?:(?:25[0-5]|2[0-4][0-9]|1?[0-9]{1,2})\.){3}(?:25[0-5]|2[0-4][0-9]|1?[0-9]{1,2})$",
        RegexOptions.Compiled);

    private bool AllIpsValid()
    {
        if (SelectedProfile == null) return false;
        if (SelectedProfile.IpEntries.Count == 0) return false; // need at least one
        // Allow empty addresses (for server auto-detect) or valid IPv4
        return SelectedProfile.IpEntries.All(ip => 
            string.IsNullOrWhiteSpace(ip.Address) || IPv4Regex.IsMatch(ip.Address));
    }

    private bool HasTargetPath() => SelectedProfile?.Targets?.Count > 0 && !string.IsNullOrWhiteSpace(SelectedProfile.Targets[0].Path);

    private bool CanApply() => SelectedProfile != null && HasTargetPath() && AllIpsValid();

    private bool CanRun() => SelectedProfile != null && HasTargetPath();
}
