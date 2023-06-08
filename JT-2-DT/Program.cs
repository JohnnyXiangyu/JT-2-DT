using JT_2_DT.ExecutionModes;
using JT_2_DT;
using JT_2_DT.Utils;

if (args.Length >= 1 && args[0] == "--benchmark") 
{
	List<string> folders = new();
	for (int i = 1; i < args.Length; i ++) 
	{
		folders.Add(args[i]);
	}
	new CorrectnessBenchmark(folders).Run();
}
else if (args.Length >= 1 && args[0] == "--baseline") 
{
	List<string> folders = new();
	for (int i = 1; i < args.Length; i ++) 
	{
		folders.Add(args[i]);
	}
	new BaselineBenchmark(folders).Run();
}
else if (args.Length >= 1 && args[0] == "--jt2dt") 
{
	try 
	{
		using TimerAgent timer = new();
		Jt2Dt.Run(args, Logger.ConsoleLogger());
	}
	catch (TimeoutException e) 
	{
		Console.WriteLine(e.Message);
	}
}
else 
{
	try 
	{
		using TimerAgent timer = new();
		FullPipeline.Run(args, Logger.ConsoleLogger());
	}
	catch (TimeoutException e) 
	{
		Console.WriteLine(e.Message);
	}
}
