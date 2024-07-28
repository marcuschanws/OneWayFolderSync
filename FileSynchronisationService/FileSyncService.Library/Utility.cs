using System.Configuration;
using System.IO;
using System.Xml.Linq;

namespace FileSync.Library
{
  public static class Utility
  {
    /// <summary>
    /// Get valid directory path based on user specified path. DEFAULT: Get directory path from App.config
    /// </summary>
    /// <returns>String of a directory folder path</returns>
    public static string GetValidDirPath(Dictionary<string, string> arguments, string key, string name)
    {
      string? resultPath;

      if (arguments.TryGetValue(key, out string? path))
        resultPath = EnsureValidPath(path, name);
      else
      {
        string? logPath = ConfigurationManager.AppSettings[key];
        resultPath = EnsureValidPath(logPath, name);
      }

      return resultPath;
    }

    /// <summary>
    /// Get a valid synchronisation time interval. DEFAULT: Get interval from App.config
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="key"></param>
    /// <returns>Double of the synchronisation interval between the source and replica folder</returns>
    public static double GetValidInterval(Dictionary<string, string> arguments, string key)
    {
      while (true)
      {
        if (arguments.TryGetValue("interval", out string? intervalString) && double.TryParse(intervalString, out double interval))
          return interval;
        else
        {
          intervalString = ConfigurationManager.AppSettings[key];
          if (intervalString == null || !double.TryParse(intervalString, out interval))
          {
            // Ask for user input via console app if invalid interval values were entered in both cmd line arguments and App.config
            Console.WriteLine("Folder synchronisation interval is missing or invalid. Please re-enter the synchronisation interval in seconds:");
            intervalString = Console.ReadLine();
            if (double.TryParse(intervalString, out interval))
              return interval;
          }
          else
            return interval;
        }
      }
    }

    private static string EnsureValidPath(string? path, string name)
    {
      string resultPath = path;

      while (true)
      {
        if (!IsDirPathValid(resultPath))
        {
          // Ask for user input via console app if invalid file paths were entered in both cmd line arguments and App.config
          Console.WriteLine($"Check if {name} contains invalid characters or surpasses the path character limit. Please re-enter the path:");
          resultPath = Console.ReadLine();
          if (!IsDirPathValid(resultPath))
            continue;
        }

        break; // Valid paths, exit the loop
      }

      return resultPath;
    }

    private static bool IsDirPathValid(string? path)
    {
      char[] invalidPathChars = Path.GetInvalidPathChars();
      char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
      string? fileName = Path.GetFileName(path);

      // Checking for invalid characters and length (or if app.config also contains a file path entry)
      if (string.IsNullOrEmpty(path) || path.Any(c => invalidPathChars.Contains(c)) || string.IsNullOrEmpty(fileName) ||
            fileName.Any(c => invalidFileNameChars.Contains(c)) || path.Length > 260)
        return false;

      // Check if absolute path can be returned
      try
      {
        Path.GetFullPath(path);
      }
      catch
      {
        return false;
      }

      return true;
    }
  }
}