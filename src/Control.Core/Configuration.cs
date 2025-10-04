namespace Control.Core;

public class ControlSettings
{
    public int DefaultSpeed { get; set; } = 100;
    public int MinSpeed { get; set; } = 1;
    public int MaxSpeed { get; set; } = 1000;
    public string DefaultState { get; set; } = "Idle";
    public string LogFilePath { get; set; } = "logs/control-app-.txt";
    public bool EnableConsoleLogging { get; set; } = true;
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = "https://localhost:5001";
    public int TimeoutSeconds { get; set; } = 30;
}
