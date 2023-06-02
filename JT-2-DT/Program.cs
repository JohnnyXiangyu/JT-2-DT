using JT_2_DT.ExecutionModes;
using JT_2_DT;
using JT_2_DT.Utils;

if (args.Length == 1 && args[0] == "--benchmark") 
{
	CorrectnessBenchmark.Run();
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
