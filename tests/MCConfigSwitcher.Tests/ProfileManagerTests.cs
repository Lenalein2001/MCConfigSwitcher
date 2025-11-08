using System;
using System.IO;
using FluentAssertions;
using MCConfigSwitcher.Models;
using MCConfigSwitcher.Services;
using Xunit;

namespace MCConfigSwitcher.Tests;

public class ProfileManagerTests
{
    [Fact]
    public void SaveAndLoadProfiles_Works()
    {
        var temp = Path.Combine(Path.GetTempPath(), "MCConfigSwitcher.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(temp);
        var store = Path.Combine(temp, "profiles.json");

        var pm = new ProfileManager(store);
        pm.Create("Home");
        pm.Save();

        var pm2 = new ProfileManager(store);
        pm2.Load();
        pm2.Profiles.Should().HaveCount(1);
        pm2.Profiles[0].Name.Should().Be("Home");
    }
}
