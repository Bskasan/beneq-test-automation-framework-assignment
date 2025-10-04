using Control.Core;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System;

public class ViewModelTests
{
    private readonly Mock<IInterlockService> _interlocks;
    private readonly Mock<IJobService> _jobs;
    private readonly Mock<IClock> _clock;
    private readonly Mock<ILogger<MainViewModel>> _logger;

    public ViewModelTests()
    {
        _interlocks = new Mock<IInterlockService>();
        _jobs = new Mock<IJobService>();
        _clock = new Mock<IClock>();
        _logger = new Mock<ILogger<MainViewModel>>();
    }

    private MainViewModel CreateViewModel()
    {
        return new MainViewModel(_interlocks.Object, _jobs.Object, _clock.Object, _logger.Object);
    }

    [Fact]
    public void Start_Disabled_When_Interlock_Active()
    {
        _interlocks.Setup(i => i.IsAnyActive).Returns(true);
        var vm = CreateViewModel();

        vm.StartCmd.CanExecute(null).Should().BeFalse();

        _interlocks.Setup(i => i.IsAnyActive).Returns(false);
        vm.NotifyInterlocksChanged();

        vm.StartCmd.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Speed_Validation_Works_Correctly()
    {
        var vm = CreateViewModel();

        // Valid speed
        vm.Speed = 500;
        vm.Speed.Should().Be(500);

        // Invalid speed (too low)
        vm.Speed = 0;
        vm.Speed.Should().Be(500); // Should remain unchanged

        // Invalid speed (too high)
        vm.Speed = 1001;
        vm.Speed.Should().Be(500); // Should remain unchanged
    }

    [Fact]
    public void SpeedText_Updates_Speed_Correctly()
    {
        var vm = CreateViewModel();

        vm.SpeedText = "250";
        vm.Speed.Should().Be(250);

        vm.SpeedText = "invalid";
        vm.Speed.Should().Be(250); // Should remain unchanged
    }

    [Fact]
    public async Task StartAsync_Logs_And_Updates_Status()
    {
        _interlocks.Setup(i => i.IsAnyActive).Returns(false);
        var vm = CreateViewModel();
        vm.Speed = 300;

        await vm.StartAsync();

        _jobs.Verify(j => j.StartAsync(300), Times.Once);
        vm.Status.Should().Be("Running");
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting job with speed 300")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_Blocks_When_Interlock_Active()
    {
        _interlocks.Setup(i => i.IsAnyActive).Returns(true);
        var vm = CreateViewModel();

        await vm.StartAsync();

        _jobs.Verify(j => j.StartAsync(It.IsAny<int>()), Times.Never);
        vm.Status.Should().Be("Idle");
    }

    [Fact]
    public async Task StartAsync_Handles_Exceptions()
    {
        _interlocks.Setup(i => i.IsAnyActive).Returns(false);
        _jobs.Setup(j => j.StartAsync(It.IsAny<int>())).ThrowsAsync(new Exception("Test exception"));
        var vm = CreateViewModel();

        await vm.StartAsync();

        vm.Status.Should().Be("Error");
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to start job")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_Logs_And_Updates_Status()
    {
        var vm = CreateViewModel();
        // Start first to set status to Running
        await vm.StartAsync();

        await vm.StopAsync();

        _jobs.Verify(j => j.StopAsync(), Times.Once);
        vm.Status.Should().Be("Idle");
    }

    [Fact]
    public void EStop_Updates_Status_To_Idle()
    {
        var vm = CreateViewModel();
        // Start first to set status to Running
        vm.StartAsync().Wait();

        vm.EStop();

        vm.Status.Should().Be("Idle");
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Emergency stop activated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void StopCmd_Enabled_Only_When_Running()
    {
        var vm = CreateViewModel();

        vm.Status.Should().Be("Idle");
        vm.StopCmd.CanExecute(null).Should().BeFalse();

        // Start to set status to Running
        vm.StartAsync().Wait();
        vm.Status.Should().Be("Running");
        vm.StopCmd.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void NotifyInterlocksChanged_Updates_InterlockActive()
    {
        var vm = CreateViewModel();
        vm.InterlockActive.Should().BeFalse();

        _interlocks.Setup(i => i.IsAnyActive).Returns(true);
        vm.NotifyInterlocksChanged();

        vm.InterlockActive.Should().BeTrue();
    }
}
