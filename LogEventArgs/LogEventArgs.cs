namespace myappmvc.LogEventArgs
{
    public class LogEventArgs : EventArgs
    {
        public LogLevel Level { get; set; }  
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; } 
    }

    public enum LogLevel
    {
        Info
    }
}
