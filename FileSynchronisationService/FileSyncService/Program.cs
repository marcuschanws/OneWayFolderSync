using FileSync.Library;
using FileSync.Library.Interfaces;
using FileSync.Library.Logger;
using FileSync.Library.Services;
using System;
using System.Reflection;
using System.Xml.Linq;

namespace FileSync
{
  public static class Program
  {
    private const string _sourceFolderPath = "source";
    private const string _destFolderPath = "destination";
    private const string _logFolderPath = "log";
    private const string _syncInterval = "interval";

    public static async Task Main(string[] args)
    {
      PrintInstructions();

      const string askContinue = "Continue? (Y/n): ";
      if (!ShouldContinue(askContinue))
        return;

      if (args.Length == 0)
      {
        Console.WriteLine("Missing command line arguments, please remember to fill in the arguments next time for a smoother experience. Continuing with program...");
        Console.WriteLine();
      }

      try
      {
        Dictionary<string, string> arguments = ParseArguments(args);

        ILogger logger = InitLogger(arguments);
        var service = GetService(logger, arguments);
        if (service != null)
          await service.Execute().ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }

      if (System.Diagnostics.Debugger.IsAttached)
      {
        ReadKey();
      }
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
      Dictionary<string, string> arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach (string arg in args)
      {
        string[] parts = arg.Split(['='], 2);
        if (parts.Length == 2)
        {
          arguments[parts[0].ToLower()] = parts[1];
        }
      }

      return arguments;
    }

    private static ConsoleKeyInfo ReadKey()
    {
      while (Console.KeyAvailable)
        Console.ReadKey();

      return Console.ReadKey();
    }

    private static bool ShouldContinue(string askContinue)
    {
      Console.Write(askContinue);
      var key = ReadKey();
      Console.WriteLine();
      if (key.KeyChar == 'Y' || key.KeyChar == 'y' || key.KeyChar == '\r')
        return true;

      return false;
    }

    public static BaseService? GetService(ILogger logger, Dictionary<string, string> arguments)
    {
      string sourceFolderPath = InitSyncFolders(arguments, _sourceFolderPath, "Source folder path", logger);
      string destinationFolderPath = InitSyncFolders(arguments, _destFolderPath, "Destination folder path", logger);
      double interval = InitInterval(arguments, _syncInterval, logger);

      if (string.IsNullOrEmpty(sourceFolderPath) || string.IsNullOrEmpty(destinationFolderPath))
      {
        logger.Log(null, $"Sync folder paths missing.");
        return null;
      }

      var service = new FileSyncService(sourceFolderPath, destinationFolderPath, interval, logger);

      return service;
    }

    private static ILogger InitLogger(Dictionary<string, string> arguments)
    {
      string logPathDir = Utility.GetValidDirPath(arguments, _logFolderPath, "Log folder path");
      Console.WriteLine($"Log folder: {Path.GetFullPath(logPathDir)}");
      ILogger logger = new ToolLogger(logPathDir, true);

      return logger;
    }

    private static string InitSyncFolders(Dictionary<string, string> arguments, string key, string name, ILogger logger)
    {
      string pathDir = Utility.GetValidDirPath(arguments, key, name);
      logger.Log(null, $"{name}: {Path.GetFullPath(pathDir)}");

      if (!Directory.Exists(pathDir))
      {
        Directory.CreateDirectory(pathDir);
      }

      return pathDir;
    }

    private static double InitInterval(Dictionary<string, string> arguments, string key, ILogger logger)
    {
      double interval = 0;
      interval = Utility.GetValidInterval(arguments, key);
      logger.Log(null, $"Synchronisation Interval: {interval} seconds");

      return interval;
    }

    public static void PrintInstructions()
    {
      Console.WriteLine($"Starting {Assembly.GetExecutingAssembly().FullName}.");
      Console.WriteLine();
      Console.WriteLine("Tool to synchronise the contents of a source folder with a destination folder at specified time intervals");
      Console.WriteLine();
      Console.WriteLine("Example Command:");
      Console.WriteLine($"{_sourceFolderPath}=\"C:\\Path\\To\\SourceFolder\" {_destFolderPath}=\"C:\\Path\\To\\DestinationFolder\" {_logFolderPath}=\"C:\\Path\\To\\LogFolder\" {_syncInterval}=60 ");
      Console.WriteLine("  Destination folder will be a full, identical copy of the source folder after every 60 seconds and file change operations will be logged to the specified folder and to the console output");
      Console.WriteLine("  If any of the above input parameters are omitted, the ones from App.config will be used.");
      Console.WriteLine();
    }
  }
}