using System.Diagnostics;

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
		
		foreach (string[] args in AllConfigs()) 
		{
			FullPipeline.Run(args);
		}
	}
}
