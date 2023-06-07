namespace JT_2_DT;

public class Logger 
{
	private List<string> _logEntries = new();
	private Action<string> _loggingAction;
	
	public Logger() 
	{
		_loggingAction = (x) => _logEntries.Add(x);
	}
	
	public Logger(Action<string> loggingAction) 
	{
		_loggingAction = loggingAction;
	}
	
	public static Logger ConsoleLogger() 
	{
		return new((x) => Console.Error.WriteLine(x));
	}
	
	public void LogInformation(string entry) 
	{
		_loggingAction(entry);
	}
}
