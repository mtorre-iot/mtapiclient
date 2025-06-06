namespace gaihcc2dataserver.classes;
using gaihcc2dataserver.common;

/// <summary>
/// Log Level
/// </summary>
public enum logLevel : ushort
{
    /// <summary>
    /// Critical Error Logs (least verbose).
    /// </summary>
    critical = 0,

    /// <summary>
    /// Error
    /// </summary>
    error = 1,

    /// <summary>
    /// Warning
    /// </summary>
    warning = 2,

    /// <summary>
    /// Information.
    /// </summary>
    info = 3,

    /// <summary>
    /// Debug
    /// </summary>
    debug = 4,

    /// <summary>
    /// Trace log (very verbose)
    /// </summary>
    trace = 5
}

/// <summary>
/// Provides Logging methods
/// </summary>
public static class Logger
{
    private static readonly object _lock = new object();
    private static volatile logLevel _level = logLevel.warning;
    private static volatile string _appName = "";

    public static logLevel level 
    { 
        get => _level;
        set => _level = value;
    }

    public static string AppName
    {
        get => _appName;
        set => _appName = value;
    }

    /// <summary>
    /// Initilize the logger
    /// </summary>
    public static void Init(string log_level)
    {
        lock (_lock)
        {
            string? envlevel = CommonUtilities.GetEnvVariableWithDefault("LOG_LEVEL", log_level);
            
            if (envlevel != null)
            {
                envlevel = envlevel.ToLower();
                if (Enum.IsDefined(typeof(logLevel), envlevel))
                {
                    _level = (logLevel)Enum.Parse(typeof(logLevel), envlevel);
                }
                else
                    write(logLevel.critical, $"Invalid log level '{envlevel}' specified. Defaulting to 'info'.");
            }
        }
    }
    public static string GetCurrentLevel()
    {
        return _level.ToString();
    }

    /// <summary>
    /// Writes a log in sensia format
    /// </summary>
    /// <param name="lvl">Log Level </param>
    /// <param name="log">String message of the log</param>
    public static void write(logLevel lvl, string log)
    {
        lock (_lock)
        {
            if (lvl <= _level)
            {
                string date = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                Console.WriteLine($"[0][{date}][{_appName}][{lvl}]{log}");
            }
        }
    }
}
