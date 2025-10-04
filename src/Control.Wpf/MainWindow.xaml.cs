using System.Windows;
using Control.Core;

namespace Control.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

file sealed class DemoInterlocks : IInterlockService
{
    public bool IsAnyActive { get; private set; }
    public void SetActive(bool active) => IsAnyActive = active;
}

file sealed class DemoJobs : IJobService
{
    public Task StartAsync(int speed) => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
}
