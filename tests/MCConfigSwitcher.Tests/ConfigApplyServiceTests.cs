using System;
using System.IO;
using FluentAssertions;
using MCConfigSwitcher.Models;
using MCConfigSwitcher.Services;
using Xunit;

namespace MCConfigSwitcher.Tests;

public class ConfigApplyServiceTests
{
    [Fact]
    public void DryRunAndApply_LineKey_UpdatesServerIp()
    {
        var dir = Path.Combine(Path.GetTempPath(), "MCConfigSwitcher.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "server.properties");
        File.WriteAllText(file, "level-name=world\nserver-ip=\nmax-players=20\n");

        var profile = new Profile
        {
            Name = "Home",
            Targets =
            {
                new TargetFile
                {
                    Path = file,
                    MakeBackup = true,
                    Rules =
                    {
                        new ReplacementRule
                        {
                            Kind = RuleKind.LineKey,
                            LineKey = "server-ip",
                            Value = "192.168.0.10"
                        }
                    }
                }
            }
        };

        var svc = new ConfigApplyService();
        var dry = svc.DryRun(profile);
        dry.FileDiffs[file].Should().NotBe("(no changes)");

        var apply = svc.Apply(profile);
        apply.Success.Should().BeTrue();
        var text = File.ReadAllText(file);
        text.Should().Contain("server-ip=192.168.0.10");
    }
}
