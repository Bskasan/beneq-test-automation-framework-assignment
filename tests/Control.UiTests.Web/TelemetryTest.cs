using Microsoft.Playwright;
using Xunit;

public class TelemetryTest
{
    [Fact(Skip="Optional web demo; remove Skip to enable")]
    public async Task TelemetryCard_Shows_Current_Job()
    {
        using var pw = await Playwright.CreateAsync();
        await using var browser = await pw.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://localhost:5001/dashboard");
        await page.GetByRole(AriaRole.Heading, new() { Name = "Telemetry" }).IsVisibleAsync();
        await page.GetByTestId("current-job").IsVisibleAsync();
    }
}
