using JT_2_DT.ExecutionModes;
using JT_2_DT;

if (args.Length == 0) 
{
	CorrectnessBenchmark.Run();
}
else 
{
	try 
	{
		FullPipeline.Run(args, Logger.ConsoleLogger());
	}
	catch (TimeoutException e) 
	{
		Console.WriteLine(e.Message);
	}
}
