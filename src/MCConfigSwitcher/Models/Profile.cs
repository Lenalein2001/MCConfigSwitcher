using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MCConfigSwitcher.Models;

public class Profile : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    private string _name = "New Profile";
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    
    public MatchRules Match { get; set; } = new();
    public Dictionary<string, string> Variables { get; set; } = new();
    public ObservableCollection<TargetFile> Targets { get; set; } = new();
    public ObservableCollection<IpEntry> IpEntries { get; set; } = new();
    // Active IP choice retained for backward compatibility; may be unused when using dynamic IP entries.
    private string _activeIpChoice = "Home";
    public string ActiveIpChoice { get => _activeIpChoice; set => SetProperty(ref _activeIpChoice, value); }
}
