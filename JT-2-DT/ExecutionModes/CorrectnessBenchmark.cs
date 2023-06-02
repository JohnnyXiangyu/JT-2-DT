using System.Text.RegularExpressions;
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
			"sat-grid-pbl-0020.cnf",
			"sat-grid-pbl-0025.cnf",
			"sat-grid-pbl-0030.cnf"
		};

		if (cnfFiles.Any(x => !File.Exists(Path.Combine("Examples", x))))
		{
			throw new FileLoadException("benchmark files not found");
		}

		string[] solvers =
		{
			"tamaki2017-heuristic",
			"flowcutter",
			"tamaki2017-exact",
			"tdlib",
		};

		string mode = "--dnnf";

		string[] cleanness =
		{
			"clean",
			"dirty"
		};

		// prepare baseline
		Dictionary<string, string> baselineModelCounts = new();
		Dictionary<string, string> baselineCompileTime = new();
		foreach (string file in cnfFiles)
		{
			baselineModelCounts[file] = "0";
			baselineCompileTime[file] = "0";
		}

		// get baseline
		Task[] baselineTasks = cnfFiles.Select(x =>
		{
			string cnfPath = Path.Combine("Examples", x);
			return Task.Run(() =>
			{
				using Process c2dInstance = new();
				c2dInstance.StartInfo.FileName = Path.Combine("external_executables", $"c2d_{Defines.OsSuffix}");
				c2dInstance.StartInfo.Arguments = $"-in {cnfPath} -count";
				c2dInstance.StartInfo.RedirectStandardOutput = true;
				c2dInstance.Start();

				string? c2dOutputLine = string.Empty;
				while ((c2dOutputLine = c2dInstance.StandardOutput.ReadLine()) != null)
				{
					ModelCountResults? result = FilterModelCount(c2dOutputLine);
					if (result != null)
					{
						baselineModelCounts[x] = result.Count;
					}
					else 
					{
						Match c2dTimeMatch = s_C2dTimePattern.Match(c2dOutputLine);
						if (c2dTimeMatch.Success) 
						{
							baselineCompileTime[x] = c2dTimeMatch.Groups["sec"].Value;
						}
					}
				}

				c2dInstance.WaitForExit();
			});
		}).ToArray();
		Task.WaitAll(baselineTasks);

		// run my version
		Console.WriteLine("Solver, CNF File, Dtree Mode, Finished?, Model Count, Correct?, DNNF Size (bytes), Time to Dtree (ms), Time to NNF/Model Count (ms), Vanilla c2d Compile Time (sec)"); // csv header
		foreach (string solver in solvers)
		{
			foreach (string clean in cleanness)
			{
				foreach (string cnf in cnfFiles)
				{
					string myCount = string.Empty;
					string dtreeTime = "0";
					string dnnfTime = "0";
					Logger logger = new((x) =>
					{
						ModelCountResults? result = FilterModelCount(x);
						if (result != null)
						{
							myCount = result.Count;
							return;
						}
						
						Match timerMatch;
						if ((timerMatch = s_DtreeTimePattern.Match(x)).Success) 
						{
							dtreeTime = timerMatch.Groups["ms"].Value;
						}
						else if ((timerMatch = s_DnnfTimePattern.Match(x)).Success) 
						{
							dnnfTime = timerMatch.Groups["ms"].Value;
						}
					});
					
					FileInfo? dnnfInfo = null;
					bool finished = true;

					try {
						// run the full pipeline
						string cnfPath = Path.Combine("Examples", cnf);
						string[] args = { mode, cnfPath, "--" + clean, "--" + solver };
						FullPipeline.Run(args, logger);
						
						// calculate dnnf size
						string dnnfPath = $"{cnfPath}.nnf";
						dnnfInfo = new(dnnfPath);
					}
					catch (TimeoutException) 
					{
						finished = false;
					}
					finally 
					{
						Console.WriteLine($"{solver}, {cnf}, {clean}, {finished}, {myCount}, {myCount == baselineModelCounts[cnf]}, {dnnfInfo?.Length}, {dtreeTime}, {dnnfTime}, {baselineCompileTime[cnf]}");
					}
				}
			}
		}
	}

	// lang=regex
	static Regex s_ModelCountPattern = new(@"Counting...(?<count>\d+) models / (?<time>\d+\.\d+)s");
	static Regex s_DtreeTimePattern = new(@"\[timer\] dtree: (?<ms>.+)");
	static Regex s_DnnfTimePattern = new(@"\[timer\] dnnf: (?<ms>.+)");
	static Regex s_C2dTimePattern = new(@"Total Time: (?<sec>.+)s");

	private class ModelCountResults
	{
		public string Count { get; init; } = string.Empty;
		public string Time { get; init; } = string.Empty;
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
