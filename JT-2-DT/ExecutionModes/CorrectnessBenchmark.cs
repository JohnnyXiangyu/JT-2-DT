using System.Text.RegularExpressions;
using System.Diagnostics;

namespace JT_2_DT.ExecutionModes;

public class CorrectnessBenchmark
{
	public static void Run()
	{
		List<string> cnfFiles = LoadBenchmarks();
		Console.Error.WriteLine("loaded all benchmarks:");
		foreach (string file in cnfFiles) 
		{
			Console.Error.Write("    ");
			Console.Error.WriteLine(file);
		}
		Console.Error.WriteLine("");

		string[] solvers =
		{
			"tamaki2017-heuristic",
			// "flowcutter",
			// "htd",
			// "tamaki2017-exact",
			"tdlib-exact",
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
		Dictionary<string, Task> baselineTasksByFile = new();
		foreach (string cnfFile in cnfFiles)
		{
			baselineTasksByFile[cnfFile] = RunBaseline(cnfFile, baselineModelCounts, baselineCompileTime);
		}

		// csv header
		Console.WriteLine("Solver, CNF File, Dtree Mode, Finished?, Model Count, Correct?, DNNF Size (bytes), Time to Dtree (ms), Time to NNF/Model Count (ms), Vanilla c2d Compile Time (sec)");
		
		// spawn instance tasks
		List<Task> instanceTasks = new();
		foreach (string solver in solvers)
		{
			foreach (string clean in cleanness)
			{
				foreach (string cnfPath in cnfFiles)
				{
					Task instanceTask = Task.Run(async () =>
					{
						// configure the logger
						string myCount = string.Empty;
						string dtreeTime = "0";
						string dnnfTime = "0";
						
						// configure logger
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
								Console.Error.WriteLine($"{solver}.{clean}.{cnfPath} {x}");
							}
							else if ((timerMatch = s_DnnfTimePattern.Match(x)).Success)
							{
								dnnfTime = timerMatch.Groups["ms"].Value;
								Console.Error.WriteLine($"{solver}.{clean}.{cnfPath} {x}");
							}
						});

						// prepare to get stats from dnnf
						FileInfo? dnnfInfo = null;
						bool finished = true;
						
						// request temp files to compile and count models (this is needed since c2d doesn't allow renaming of output files)
						using Utils.TempFileAgent tempCnf = new();
						
						// copy input cnf to target file
						using (FileStream inCnf = File.OpenRead(cnfPath))
						using (FileStream outCnf = File.OpenWrite(tempCnf.TempFilePath))
						{
							inCnf.CopyTo(outCnf);
						}
						
						// run the main routine
						try 
						{
							string[] args = { mode, tempCnf.TempFilePath, "--" + clean, "--" + solver };
							FullPipeline.Run(args, logger);

							// calculate dnnf size
							string dnnfPath = $"{tempCnf.TempFilePath}.nnf";
							if (File.Exists(dnnfPath))
								dnnfInfo = new(dnnfPath);
						}
						catch (TimeoutException)
						{
							finished = false;
							Console.Error.WriteLine($"{solver}.{clean}.{cnfPath} timeout");
						}
						finally
						{
							await baselineTasksByFile[cnfPath];
							Console.WriteLine($"{solver}, {cnfPath}, {clean}, {finished}, {myCount}, {myCount == baselineModelCounts[cnfPath]}, {dnnfInfo?.Length}, {dtreeTime}, {dnnfTime}, {baselineCompileTime[cnfPath]}");
							
							if (File.Exists($"{tempCnf.TempFilePath}.nnf")) 
							{
								File.Delete($"{tempCnf.TempFilePath}.nnf");
							}
						}
					});
					instanceTasks.Add(instanceTask);
				}
			}
		}
		
		Task.WaitAll(instanceTasks.ToArray());
		Console.Error.WriteLine("done");
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
	
	private static List<string> LoadBenchmarks() 
	{
		List<string> cnfFiles = new();
		
		DirectoryInfo d = new DirectoryInfo("Examples");
		FileInfo[] Files = d.GetFiles("*.cnf");

		foreach(FileInfo file in Files )
		{
			cnfFiles.Add(file.FullName);
		}

		if (cnfFiles.Any(x => !File.Exists(Path.Combine("Examples", x))))
		{
			throw new FileLoadException("benchmark files not found");
		}
		
		return cnfFiles;
	}
	
	private static Task RunBaseline(string cnfFile, Dictionary<string, string> baselineModelCounts, Dictionary<string, string> baselineCompileTime) 
		=> Task.Run(async () =>
	{
		using Utils.TempFileAgent tempCnf = new();
		string cnfPath = tempCnf.TempFilePath;
		{
			using FileStream originalCnf = File.OpenRead(cnfFile);
			using FileStream tempCnfFile = File.OpenWrite(tempCnf.TempFilePath);
			await originalCnf.CopyToAsync(tempCnfFile);
		}
		
		using Process c2dInstance = new();
		c2dInstance.StartInfo.FileName = Path.Combine("external_executables", $"c2d_{Defines.OsSuffix}");
		c2dInstance.StartInfo.Arguments = $"-in {cnfPath} -count -smooth_all -reduce";
		c2dInstance.StartInfo.RedirectStandardOutput = true;
		c2dInstance.Start();

		string? c2dOutputLine = string.Empty;
		while ((c2dOutputLine = c2dInstance.StandardOutput.ReadLine()) != null)
		{
			ModelCountResults? result = FilterModelCount(c2dOutputLine);
			if (result != null)
			{
				baselineModelCounts[cnfFile] = result.Count;
			}
			else
			{
				Match c2dTimeMatch = s_C2dTimePattern.Match(c2dOutputLine);
				if (c2dTimeMatch.Success)
				{
					baselineCompileTime[cnfFile] = c2dTimeMatch.Groups["sec"].Value;
				}
			}
		}

		c2dInstance.WaitForExit();
		if (File.Exists(cnfPath + ".nnf")) 
		{
			File.Delete(cnfPath + ".nnf");
		}
		
		Console.Error.WriteLine($"baseline finished: {cnfFile}");
	});
}
