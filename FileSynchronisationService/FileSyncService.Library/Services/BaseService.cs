using FileSync.Library.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSync.Library.Services
{
  public abstract class BaseService
  {
    private ILogger _logger;

    protected BaseService(ILogger logger)
    {
      _logger = logger;
    }

    public async Task Execute()
    {
      await OnExecute().ConfigureAwait(false);
    }

    protected abstract Task OnExecute();

    protected void Log(string message)
    {
      _logger?.Log(string.Empty, message);
    }

    protected void LogError(Exception ex)
    {
      _logger?.Log("ERR", ex.Message, ex);
    }

    protected void Progress(string message)
    {
      Log(message);
    }
  }
}