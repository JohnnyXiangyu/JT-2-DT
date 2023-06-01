using JT_2_DT.ExecutionModes;
using JT_2_DT;

if (args.Length == 0) 
{
	CorrectnessBenchmark.Run();
}
else 
{
	FullPipeline.Run(args, Logger.ConsoleLogger());
}
