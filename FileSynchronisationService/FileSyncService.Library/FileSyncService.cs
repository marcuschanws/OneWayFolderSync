using FileSync.Library.Interfaces;
using FileSync.Library.Services;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Timers;

namespace FileSync.Library
{
  public class FileSyncService : BaseService
  {
    protected static FileInfo FileInfoSource;
    protected static FileInfo FileInfoDest;

    private static string _sourcePath;
    private static string _destPath;
    private static double _intervalInSecs;
    private static System.Timers.Timer _syncTimer;

    public FileSyncService(string sourcePath, string destPath, double intervalInSecs, ILogger logger)
  : base(logger)
    {
      _sourcePath = sourcePath;
      _destPath = destPath;
      _intervalInSecs = intervalInSecs;

      // Initialise and start the timer for synchronisation
      _syncTimer = new System.Timers.Timer(_intervalInSecs * 1000);
      _syncTimer.Elapsed += async (sender, e) => await FileSyncAction();
      _syncTimer.AutoReset = true;
      _syncTimer.Enabled = true;
    }

    protected override async Task OnExecute()
    {
      //Initial synchronisation when tool is executed
      await FileSyncAction().ConfigureAwait(false);
    }

    /// <summary>
    /// Synchronisation between the source and destination/replica folder, depending on given time interval, done here
    /// </summary>
    private async Task FileSyncAction()
    {
      try
      {
        //Ensure that source and destination folders will be recreated if they get deleted during runtime
        EnsureSourceAndDestFoldersExist();

        string[] sourceFiles = Directory.GetFiles(_sourcePath, "*.*", SearchOption.AllDirectories);
        string[] destFiles = Directory.GetFiles(_destPath, "*.*", SearchOption.AllDirectories);

        // Obtain relative file paths
        var sourceFilesRelative = sourceFiles.Select(s => s.Substring(_sourcePath.Length + 1)).ToHashSet();
        var destFilesRelative = destFiles.Select(d => d.Substring(_destPath.Length + 1)).ToHashSet();
        // Find files to copy
        var filesToCopy = sourceFilesRelative.Except(destFilesRelative).ToList();
        var filesToDelete = destFilesRelative.Except(sourceFilesRelative).ToList();

        await CopyFilesFromSourceToDest(filesToCopy);

        string[] sourceDirectories = Directory.GetDirectories(_sourcePath, "*", SearchOption.AllDirectories);
        string[] destDirectories = Directory.GetDirectories(_destPath, "*", SearchOption.AllDirectories);
        // Obtain relative directory paths
        var sourceDirectoriesRelative = sourceDirectories.Select(s => s.Substring(_sourcePath.Length + 1)).ToHashSet();
        var destDirectoriesRelative = destDirectories.Select(s => s.Substring(_destPath.Length + 1)).ToHashSet();

        DeleteFilesAndDirsFromDest(filesToDelete, sourceDirectoriesRelative, destDirectoriesRelative);
        await UpdateFilesInDest(sourceFilesRelative, destFilesRelative);
      }
      catch (Exception ex)
      {
        LogError(ex);
      }
      finally
      {
        Log($"File Synchronisation done. Awaiting for the next synchronisation interval...");
      }
    }

    /// <summary>
    /// Copy files from source to destination folder
    /// </summary>
    /// <param name="filesToCopy"></param>
    private async Task CopyFilesFromSourceToDest(List<string> filesToCopy)
    {
      foreach (var file in filesToCopy)
      {
        try
        {
          var sourceFile = Path.Combine(_sourcePath, file);
          var destFile = Path.Combine(_destPath, file);
          var destFileDir = Path.GetDirectoryName(destFile);

          if (destFileDir != null && !Directory.Exists(destFileDir))
            Directory.CreateDirectory(destFileDir);

          await CopyFileAsync(sourceFile, destFile);
          Log($"File copied from source: {file} and created in destination folder: {destFile}");
        }
        catch (Exception ex)
        {
          Log($"Error copying following file from source: {file}");
          LogError(ex);
        }
      }
    }

    /// <summary>
    /// Update files that have been altered or changed
    /// </summary>
    /// <param name="sourceFilesRelative"></param>
    /// <param name="destFilesRelative"></param>
    /// <returns></returns>
    private async Task UpdateFilesInDest(HashSet<string> sourceFilesRelative, HashSet<string> destFilesRelative)
    {
      var commonFiles = sourceFilesRelative.Intersect(destFilesRelative).ToList();
      foreach (var file in commonFiles)
      {
        try
        {
          FileInfoSource = new FileInfo(Path.Combine(_sourcePath, file));
          FileInfoDest = new FileInfo(Path.Combine(_destPath, file));

          IsFileInfosExist();

          if (!IsFilesIdentical())
          {
            await CopyFileAsync(FileInfoSource.FullName, FileInfoDest.FullName);
            Log($"File updated in destination folder: {file}");
          }
        }
        catch (Exception ex)
        {
          Log($"Error updating the following file: {file}");
          LogError(ex);
        }
      }
    }

    private static async Task CopyFileAsync(string sourceFile, string destFile)
    {
      using (FileStream sourceStream = File.Open(sourceFile, FileMode.Open))
      using (FileStream destStream = File.Create(destFile))
      {
        await sourceStream.CopyToAsync(destStream);
      }
    }

    /// <summary>
    /// Delete files and directories from destination folder that are not in source
    /// </summary>
    /// <param name="filesToDelete"></param>
    /// <param name="sourceDirectoriesRelative"></param>
    /// <param name="destDirectoriesRelative"></param>
    /// <returns></returns>
    private void DeleteFilesAndDirsFromDest(List<string> filesToDelete, HashSet<string> sourceDirectoriesRelative, HashSet<string> destDirectoriesRelative)
    {
      //Delete files
      foreach (var file in filesToDelete)
      {
        try
        {
          var destFile = Path.Combine(_destPath, file);

          File.Delete(destFile);
          Log($"File deleted from destination folder: {file}");
        }
        catch (Exception ex)
        {
          Log($"Error deleting following file from destination folder: {file}");
          LogError(ex);
        }
      }

      // Delete directories
      var directoriesToDelete = destDirectoriesRelative.Except(sourceDirectoriesRelative).OrderByDescending(d => d.Length).ToList();
      foreach (var dir in directoriesToDelete)
      {
        try
        {
          var destSubDir = Path.Combine(_destPath, dir);

          Directory.Delete(destSubDir, true);
          Log($"Deleted directory from destination folder: {dir}");
        }
        catch (Exception ex)
        {
          Log($"Error deleting following directory from destination folder: {dir}");
          LogError(ex);
        }
      }
    }

    /// <summary>
    /// Compares the two specified files between the source and destination and returns true if the files are identical
    /// </summary>
    /// <returns>Returns true if the files are identical and false otherwise</returns>
    private bool IsFilesIdentical()
    {
      //Do early comparison of file lengths, if different, files are definitely different
      if (IsDifferentLength())
        return false;

      if (IsSameFile())
        return true;

      return OnCompareFiles();
    }

    /// <summary>
    /// Compares the two specified files to determine if they are in the same directory and returns true if the files are
    /// </summary>
    /// <returns>Returns true if the files have the exact same path</returns>
    private bool IsSameFile()
    {
      return string.Equals(FileInfoSource.FullName, FileInfoDest.FullName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checking files Length, if lengths are different, returns true
    /// </summary>
    /// <returns>Returns true if files are of different lengths and false otherwise</returns>
    private bool IsDifferentLength()
    {
      return FileInfoSource.Length != FileInfoDest.Length;
    }

    /// <summary>
    /// Compares bytes of the two given files in chunks of 8 bytes
    /// </summary>
    /// <returns>Returns true if files are identical and false otherwise</returns>
    private bool OnCompareFiles()
    {
      var fileContentsSource = File.ReadAllBytes(FileInfoSource.FullName);
      var fileContentsDest = File.ReadAllBytes(FileInfoDest.FullName);

      int lastBlockIndex = fileContentsSource.Length - (fileContentsSource.Length % sizeof(ulong));

      int totalProcessed = 0;
      while (totalProcessed < lastBlockIndex)
      {
        // Checks if bytes of file chunk is equal, if not return false
        if (BitConverter.ToUInt64(fileContentsSource, totalProcessed) != BitConverter.ToUInt64(fileContentsDest, totalProcessed))
          return false;

        totalProcessed += sizeof(ulong);
      }

      return true;
    }

    private void EnsureSourceAndDestFoldersExist()
    {
      if (!Directory.Exists(_sourcePath))
      {
        Log($"{_sourcePath} could not be found. Directory created.");
        Directory.CreateDirectory(_sourcePath);
      }

      if (!Directory.Exists(_destPath))
      {
        Log($"{_destPath} could not be found. Directory created.");
        Directory.CreateDirectory(_destPath);
      }
    }

    private void IsFileInfosExist()
    {
      if (FileInfoSource.Exists == false)
        LogError(new ArgumentNullException(nameof(FileInfoSource)));

      if (FileInfoDest.Exists == false)
        LogError(new ArgumentNullException(nameof(FileInfoDest)));
    }
  }
}