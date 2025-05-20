using log4net;
using myappmvc.LogEventArgs;

public class Log4NetEventHandler
{
    private readonly ILog _logger = LogManager.GetLogger(typeof(Log4NetEventHandler));

    public void Handle(object? sender, LogEventArgs e)
    {
        switch (e.Level)
        {
            case myappmvc.LogEventArgs.LogLevel.Info:
                _logger.Info(e.Message);
                break;
        }


    }

}
//nhận và xử lý sự kiện log sau đó chuyển đến log4net ghi log