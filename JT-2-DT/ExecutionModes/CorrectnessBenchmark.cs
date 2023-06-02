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
		
		Task baselineTask = Task.Run(() => 
		{
			
		});
		
		foreach (string cnf in cnfFiles) 
		{
			foreach (string solver in solvers) 
			{
				foreach (string clean in cleanness) 
				{
					Logger logger = new((x) => 
					{
						ModelCountResults? result = FilterModelCount(x);
						if (result != null) 
						{
							Console.WriteLine($"{solver}, {cnf}, {clean}, {result?.Count}");
						}
					});
					
					string cnfPath = Path.Combine("Examples", cnf);
					string[] args = {mode, cnfPath, clean, solver};
					FullPipeline.Run(args, logger);
				}
			}
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
