using log4net;
using myappmvc.Interfaces;
using myappmvc.LogEventArgs;

namespace myappmvc.Services
{
    public class LoggerService : ILoggerService
    {
        private readonly EventBasedLogger _eventLogger;
        private readonly ILog _log;

        public LoggerService(EventBasedLogger eventLogger)
        {
            _eventLogger = eventLogger;
            _log = LogManager.GetLogger(typeof(LoggerService));
        }

        public void Info(object sender, string message)
        {
            _eventLogger.Info(sender, message);
        }
            
        public void Warn(object sender, string message)
        {
            _log.Warn(message);
        }

        public void Error(object sender, string message, Exception? ex = null)
        {
            if (ex != null)
                _log.Error(message, ex);
            else
                _log.Error(message);
        }
    }
}
