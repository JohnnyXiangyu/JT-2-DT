using JT_2_DT.ExecutionModes;

if (args.Length == 0) 
{
	CorrectnessBenchmark.Run();
}
else 
{
	FullPipeline.Run(args);
}
