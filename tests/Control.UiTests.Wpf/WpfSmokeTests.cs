using System;
using System.IO;
using FlaUI.Core;
using FlaUI.UIA3;
using FluentAssertions;
using Xunit;

public class WpfSmokeTests
{
    [Fact]
    public void Start_Stop_Changes_Status()
    {
        var exe = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Control.Wpf", "bin", "Debug", "net9.0-windows", "Control.Wpf.exe"));
        using var app = Application.Launch(exe);
        using var a = new UIA3Automation();
        var w = app.GetMainWindow(a);
        var start = w.FindFirstDescendant(cf => cf.ByAutomationId("StartBtn")).AsButton();
        var stop = w.FindFirstDescendant(cf => cf.ByAutomationId("StopBtn")).AsButton();
        var status = w.FindFirstDescendant(cf => cf.ByAutomationId("StatusText")).AsLabel();

        start.Invoke();
        status.Text.Should().Be("Running");
        stop.Invoke();
        status.Text.Should().Be("Idle");
    }
}
