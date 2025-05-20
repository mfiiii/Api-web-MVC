namespace myappmvc.LogEventArgs
{
    public class EventBasedLogger
    {
        public event EventHandler<LogEventArgs>? LogRaised;

        public void RaiseLog(object sender, LogEventArgs e)
        {
            LogRaised?.Invoke(sender, e); //gửi log ra nơi đăng ký
        }


        public void Info(object sender, string message )//sender là đối tượng phát sinh sự kiện(this)
        {
            RaiseLog(sender, new LogEventArgs
            {
                Level = LogLevel.Info,
                Message = message,
                Exception = null
            });
        }
        

    }
}
// phát sự kiện log