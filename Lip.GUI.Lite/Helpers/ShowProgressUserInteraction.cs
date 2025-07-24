using Lip.Context;
using Lip.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui;

namespace Lip.GUI.Lite.Helpers
{
    internal class ShowProgressUserInteraction(IContentDialogService dialogService) : UserInteraction(dialogService), ILogger, IUserInteraction
    {
        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }

        public event Action<float, string> InstallStateUpdate = delegate { };

        IDisposable? ILogger.BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return logLevel > LogLevel.Debug;
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId == LogEventID.PackageInstalled || eventId == LogEventID.InstallingPackage || eventId == LogEventID.RunningScript)
            {
                var logMessage = formatter(state, exception);
                InstallStateUpdate.Invoke(-1f, logMessage);
            }
            else
            {
            }
        }

        async Task IUserInteraction.UpdateProgress(string id, float progress, string format, params object[] args)
        {
            // This method can be overridden to provide specific progress update logic  
            // For now, we will just log the progress  
            var message = string.Format(format, args);
            InstallStateUpdate.Invoke(progress, message);
            await Task.CompletedTask;

        }
    }
}