using System;
using hMailServer.Core;
using hMailServer.Core.Logging;
using NLog;

using LogLevel = hMailServer.Core.Logging.LogLevel;

namespace hMailServer.Application
{
    class Log : ILog
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public void LogInfo(LogEvent logEvent)
        {
            var logEventInfo = CreateNlogEventInfo(logEvent);

            Logger.Log(logEventInfo);
        }

        public void LogApplicationInfo(string message)
        {
            LogInfo(new LogEvent
            {
                Message = message,
                EventType = LogEventType.Application,
                Protocol = null,
                RemoteEndpoint = null,
                SessionId = null,
                LogLevel = LogLevel.Info
            });
        }

        public void LogException(LogEvent logEvent, Exception exception)
        {
            var logEventInfo = CreateNlogEventInfo(logEvent);
            logEventInfo.Exception = exception;
            Logger.Log(logEventInfo);
        }

        private static LogEventInfo CreateNlogEventInfo(LogEvent logEvent)
        {
            var logEventInfo = new LogEventInfo();

            switch (logEvent.LogLevel)
            {
                case LogLevel.Info:
                    logEventInfo.Level = NLog.LogLevel.Info;
                    break;
                case LogLevel.Warning:
                    logEventInfo.Level = NLog.LogLevel.Warn;
                    break;
                case LogLevel.Error:
                    logEventInfo.Level = NLog.LogLevel.Error;
                    break;
                case LogLevel.Debug:
                    logEventInfo.Level = NLog.LogLevel.Debug;
                    break;
                default:
                    throw new NotImplementedException(string.Format("Unsupported log level: {0}", logEvent.LogLevel));
            }

            logEventInfo.Message = logEvent.Message;

            return logEventInfo;
        }
    }
}
