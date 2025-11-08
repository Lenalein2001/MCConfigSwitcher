using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using MCConfigSwitcher.Models;

namespace MCConfigSwitcher.Services;

public class ProfileManager
{
    private readonly string _storagePath;
    public List<Profile> Profiles { get; private set; } = new();

    public ProfileManager(string? storagePath = null)
    {
        _storagePath = storagePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MCConfigSwitcher", "profiles.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_storagePath)!);
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(_storagePath)) { Profiles = new List<Profile>(); return; }
            var json = File.ReadAllText(_storagePath);
            Profiles = JsonConvert.DeserializeObject<List<Profile>>(json) ?? new List<Profile>();
        }
        catch (Exception)
        {
            // If the profiles file is corrupt or unreadable, back it up and start fresh
            try
            {
                var backup = Path.Combine(Path.GetDirectoryName(_storagePath)!,
                    $"profiles.corrupt_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.Copy(_storagePath, backup, overwrite: true);
            }
            catch { /* ignore backup failures */ }
            
            Profiles = new List<Profile>();
            // Avoid throwing to prevent app crash at startup; caller can continue with empty set.
        }
    }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(Profiles, Formatting.Indented);
        File.WriteAllText(_storagePath, json);
    }

    public Profile Create(string name)
    {
        var p = new Profile { Name = name };
        Profiles.Add(p);
        return p;
    }

    public bool Delete(string id)
    {
        var idx = Profiles.FindIndex(p => p.Id == id);
        if (idx < 0) return false;
        Profiles.RemoveAt(idx);
        return true;
    }
}
