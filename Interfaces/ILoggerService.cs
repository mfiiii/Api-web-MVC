namespace myappmvc.Interfaces
{
    public interface ILoggerService
    {
        void Info(object sender, string message);
        void Warn(object sender, string message);
        void Error(object sender, string message, Exception? ex = null);
    }
}
