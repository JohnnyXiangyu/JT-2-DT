using System.Diagnostics;
using System.Text.RegularExpressions;

namespace JT_2_DT.ExecutionModes;

public class CorrectnessBenchmark
{	
	public static void Run()
	{
		string[] cnfFiles = 
		{
			"sat-grid-pbl-0010.cnf",
			"sat-grid-pbl-0015.cnf",
			// "sat-grid-pbl-0020.cnf",
			// "sat-grid-pbl-0010.cnf",
			// "sat-grid-pbl-0030.cnf"
		};
		
		if (cnfFiles.Any(x => !File.Exists(Path.Combine("Examples", x)))) 
		{
			throw new FileLoadException("benchmark files not found");
		}
		
		string[] solvers = 
		{
			"--tamaki2017-heuristic",
			"--tamaki2017-Exact",
			"--flowcutter",
			"--htd"
		};
		
		string mode = "--dnnf";
		
		string[] cleanness = 
		{
			"--clean",
			"--dirty"
		};
		
		IEnumerable<string[]> AllConfigs() 
		{
			foreach (string cnf in cnfFiles) 
			{
				foreach (string solver in solvers) 
				{
					foreach (string clean in cleanness) 
					{
						string cnfPath = Path.Combine("Examples", cnf);
						yield return new string[] {mode, cnfPath, clean, solver};
					}
				}
			}
		}
		
		
		// use custom logger
		foreach (string[] args in AllConfigs()) 
		{
			Logger logger = new((x) => 
			{
				Console.WriteLine(x);
			});
			FullPipeline.Run(args, logger);
		}
	}
	
	static Regex s_ModelCountPattern = new(@"Counting...(?<count>\d+) models / (?<time>\d+\.\d+)s");
	
	private struct ModelCountResults 
	{
		public string Count { get; init; }
		public string Time { get; init; }
	}
	
	private static ModelCountResults? FilterModelCount(string line) 
	{
		Match modelCountMatch = s_ModelCountPattern.Match(line);
		if (!modelCountMatch.Success) 
		{
			return null;
		}
		
		return new ModelCountResults 
		{
			Count = modelCountMatch.Groups["count"].Value,
			Time = modelCountMatch.Groups["time"].Value
		};
	}
}
