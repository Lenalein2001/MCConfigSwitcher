using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MCConfigSwitcher.Models;

public class TargetFile : ObservableObject
{
    private string _path = string.Empty;
    public string Path { get => _path; set => SetProperty(ref _path, value); }

    private string _encodingName = "utf-8";
    public string EncodingName { get => _encodingName; set => SetProperty(ref _encodingName, value); }

    private bool _makeBackup = true;
    public bool MakeBackup { get => _makeBackup; set => SetProperty(ref _makeBackup, value); }

    private bool _createIfMissing = false;
    public bool CreateIfMissing { get => _createIfMissing; set => SetProperty(ref _createIfMissing, value); }

    public List<ReplacementRule> Rules { get; set; } = new();
}
