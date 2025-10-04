using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;

namespace Control.Core;

public interface IInterlockService
{
    bool IsAnyActive { get; }
    void SetActive(bool active);
}

public interface IJobService
{
    Task StartAsync(int speed);
    Task StopAsync();
}

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IAsyncCommand : ICommand
{
    Task ExecuteAsync(object? parameter);
    bool IsRunning { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public sealed class RelayCommand : ICommand
{
    private readonly Func<object?, bool>? _can;
    private readonly Action<object?> _exec;
    public RelayCommand(Action<object?> exec, Func<object?, bool>? can = null)
    { _exec = exec; _can = can; }
    public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _exec(parameter);
    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class AsyncRelayCommand : IAsyncCommand
{
    private readonly Func<Task> _exec;
    private readonly Func<bool>? _can;
    private bool _isRunning;
    
    public AsyncRelayCommand(Func<Task> exec, Func<bool>? can = null)
    { 
        _exec = exec; 
        _can = can; 
    }
    
    public bool IsRunning => _isRunning;
    public bool CanExecute(object? parameter) => (_can?.Invoke() ?? true) && !_isRunning;
    
    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter);
    }
    
    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        
        _isRunning = true; 
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        
        try 
        { 
            await _exec(); 
        }
        finally 
        { 
            _isRunning = false; 
            CanExecuteChanged?.Invoke(this, EventArgs.Empty); 
        }
    }
    
    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IInterlockService _interlocks;
    private readonly IJobService _jobs;
    private readonly IClock _clock;
    private readonly ILogger<MainViewModel>? _logger;
    private readonly ControlSettings _settings;

    private string _status = "Idle";
    private bool _interlockActive;
    private int _speed = 100;
    private string _speedText = "100";

    public MainViewModel(IInterlockService interlocks, IJobService jobs, IClock clock, ILogger<MainViewModel>? logger = null, ControlSettings? settings = null)
    {
        _interlocks = interlocks; 
        _jobs = jobs; 
        _clock = clock;
        _logger = logger;
        _settings = settings ?? new ControlSettings();
        
        _speed = _settings.DefaultSpeed;
        _speedText = _settings.DefaultSpeed.ToString();
        
        StartCmd = new AsyncRelayCommand(StartAsync, () => !InterlockActive);
        StopCmd = new AsyncRelayCommand(StopAsync, () => Status == "Running");
        EStopCmd = new RelayCommand(_ => EStop());
        InterlockActive = _interlocks.IsAnyActive;
        
        _logger?.LogInformation("MainViewModel initialized with default speed {Speed}", _speed);
    }

    public string Status
    {
        get => _status;
        private set { _status = value; OnPropertyChanged(); StopCmd.RaiseCanExecuteChanged(); }
    }

    public bool InterlockActive
    {
        get => _interlockActive;
        private set { _interlockActive = value; OnPropertyChanged(); StartCmd.RaiseCanExecuteChanged(); }
    }

    public int Speed
    {
        get => _speed;
        set 
        { 
            if (value >= _settings.MinSpeed && value <= _settings.MaxSpeed)
            {
                _speed = value; 
                _speedText = value.ToString();
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(SpeedText));
            }
        }
    }

    public string SpeedText
    {
        get => _speedText;
        set 
        { 
            _speedText = value;
            OnPropertyChanged();
            
            if (int.TryParse(value, out int speed) && speed >= _settings.MinSpeed && speed <= _settings.MaxSpeed)
            {
                _speed = speed;
                OnPropertyChanged(nameof(Speed));
            }
        }
    }

    public string? SpeedError { get; private set; }

    public AsyncRelayCommand StartCmd { get; }
    public AsyncRelayCommand StopCmd { get; }
    public RelayCommand EStopCmd { get; }

    public async Task StartAsync()
    {
        try
        {
            if (InterlockActive) 
            {
                _logger?.LogWarning("Start command blocked due to active interlock");
                return;
            }
            
            _logger?.LogInformation("Starting job with speed {Speed}", Speed);
            await _jobs.StartAsync(Speed);
            Status = "Running";
            _logger?.LogInformation("Job started successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to start job");
            Status = "Error";
        }
    }

    public async Task StopAsync()
    {
        try
        {
            _logger?.LogInformation("Stopping job");
            await _jobs.StopAsync();
            Status = "Idle";
            _logger?.LogInformation("Job stopped successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to stop job");
            Status = "Error";
        }
    }

    public void EStop()
    {
        _logger?.LogWarning("Emergency stop activated");
        Status = "Idle";
    }

    public void NotifyInterlocksChanged()
    {
        InterlockActive = _interlocks.IsAnyActive;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
