namespace FileSync.Library.Interfaces
{
  public interface ILogger
  {
    void Log(string category, string message, Exception ex = null);

    void Progress(string message);
  }
}