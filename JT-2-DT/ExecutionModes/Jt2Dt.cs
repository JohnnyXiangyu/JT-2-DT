using System.Diagnostics;

namespace JT_2_DT.ExecutionModes;

public class Jt2Dt 
{
	public static void Run(string[] args, Logger logger)
	{
		Stopwatch sharedTimer = Stopwatch.StartNew();
		
		// requesting temp files

		// overall arguments
		string cnfPath = string.Empty;
		string jtPath = string.Empty;
		bool useCleanBuild = false;
		string outFile = String.Empty;

		// load from input or commandline
		if (args.Length >= 2)
		{
			cnfPath = args[1];
		}
		if (args.Length >= 3)
		{
			useCleanBuild = args[2] == "--clean";
		}
		if (args.Length >= 4) 
		{
			jtPath = args[3];
		}
		if (args.Length >= 5) 
		{
			outFile = args[4];
		}

		// load input
		Cnf formula = new(cnfPath);
		logger.LogInformation($"[timer] cnf: {sharedTimer.Elapsed.TotalSeconds}");		
		
		// dtree compilation
		Dtree dtree = new(jtPath, formula.Clauses, useCleanBuild);
		logger.LogInformation($"[timer] dtree-pregen: {sharedTimer.Elapsed.TotalSeconds}");

		// output dtree
		if (outFile != string.Empty) 
		{
			File.WriteAllLines(outFile, dtree.SerializeAsDtree());
		}
		else 
		{
			foreach (string line in dtree.SerializeAsDtree())
			{
				Console.WriteLine(line);
			}
		}
		
		logger.LogInformation($"[timer] dtree: {sharedTimer.Elapsed.TotalSeconds}");
		return;
	}
}