using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Threading.Tasks;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public ApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Start_Returns_Running()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsync("/api/jobs/start?speed=120", null);
        res.EnsureSuccessStatusCode();
        var payload = await res.Content.ReadFromJsonAsync<dynamic>();
        ((string)payload!.state).Should().Be("Running");
    }

    [Fact]
    public async Task Start_Rejects_Invalid_Speed_Too_Low()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsync("/api/jobs/start?speed=0", null);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Start_Rejects_Invalid_Speed_Too_High()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsync("/api/jobs/start?speed=1001", null);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Stop_Returns_Idle()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsync("/api/jobs/stop", null);
        res.EnsureSuccessStatusCode();
        var payload = await res.Content.ReadFromJsonAsync<dynamic>();
        ((string)payload!.state).Should().Be("Idle");
    }

    [Fact]
    public async Task Status_Returns_Current_State()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/status");
        res.EnsureSuccessStatusCode();
        var payload = await res.Content.ReadFromJsonAsync<dynamic>();
        payload.Should().NotBeNull();
        payload!.state.Should().NotBeNull();
    }
}
