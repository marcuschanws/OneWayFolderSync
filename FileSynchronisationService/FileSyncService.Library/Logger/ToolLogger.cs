using FileSync.Library.Interfaces;
using System.Globalization;

namespace FileSync.Library.Logger
{
  public class ToolLogger : ILogger
  {
    private readonly string _logFilePathTemplate;
    private readonly bool _logToConsole;

    public ToolLogger(string logDirectory, bool logToConsole)
    {
      if (!Directory.Exists(logDirectory))
      {
        Directory.CreateDirectory(logDirectory);
      }

      // Create a unique file name with timestamp
      string uniqueFileName = $"Log_{{0}}{DateTime.Now:yyyyMMdd_HHmmss}.txt";
      _logFilePathTemplate = Path.Combine(logDirectory, uniqueFileName);

      _logToConsole = logToConsole;
    }

    /// <summary>
    /// Append log file, of different categories, with specified message
    /// </summary>
    /// <param name="category"></param>
    /// <param name="message"></param>
    /// <param name="ex"></param>
    public void Log(string category, string message, Exception ex = null)
    {
      try
      {
        // To distinguish between different types of logs created for future error handling purposes
        var cat = string.IsNullOrEmpty(category) ? string.Empty : $"{category}_";
        var logFilePath = string.Format(CultureInfo.InvariantCulture, _logFilePathTemplate, cat);

        string? prefixedMsg = string.IsNullOrEmpty(message) ? null : $"{DateTime.Now:T}: {message}";
        File.AppendAllText(logFilePath, prefixedMsg);
        File.AppendAllText(logFilePath, Environment.NewLine);
        if (ex != null)
        {
          File.AppendAllText(logFilePath, ex.ToString());
          File.AppendAllText(logFilePath, Environment.NewLine);
        }
        if (_logToConsole)
          Progress(message);
      }
      catch (Exception iex)
      {
        Console.WriteLine($"Failed to log error: {iex.Message}");
      }
    }

    /// <summary>
    /// Log progress on console
    /// </summary>
    /// <param name="message"></param>
    public void Progress(string message)
    {
      try
      {
        Console.WriteLine(message);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to log progress: {ex.Message}");
      }
    }
  }
}