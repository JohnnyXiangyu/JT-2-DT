using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

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
			// "tamaki2017-heuristic",
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
		Dictionary<string, double> baselineTotalTime = new();
		Dictionary<string, double> baselineCompileTime = new();
		Dictionary<string, long> baselineNnfSize = new();
		foreach (string file in cnfFiles)
		{
			baselineModelCounts[file] = "0";
			baselineCompileTime[file] = 0;
			baselineNnfSize[file] = 0;
		}

		// get baseline
		Dictionary<string, Task> baselineTasksByFile = new();
		foreach (string cnfFile in cnfFiles)
		{
			baselineTasksByFile[cnfFile] = RunBaseline(cnfFile, baselineModelCounts, baselineTotalTime, baselineCompileTime, baselineNnfSize);
		}

		// csv header		
		StringBuilder csvHeader = new();
		csvHeader.Append("Solver, ");
		csvHeader.Append("CNF, ");
		csvHeader.Append("Dtree Mode, ");
		csvHeader.Append("Completion, ");
		csvHeader.Append("NNF Size, ");
		csvHeader.Append("NNF Size Ratio to Vanilla, ");
		csvHeader.Append("Dtree Compilation Time, ");
		csvHeader.Append("DNNF Compilation Time, ");
		csvHeader.Append("DNNF Compilation Time Ratio to Vanilla, ");
		csvHeader.Append("Total Time, ");
		csvHeader.Append("Total Time ratio to Vanilla, ");
		Console.WriteLine(csvHeader.ToString());
		
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
						double dtreeTime = 0;
						double dnnfTime = 0;
						double totalTime = 0;
						
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
								dtreeTime = double.Parse(timerMatch.Groups["ms"].Value) / 1000;
								Console.Error.WriteLine($"{solver}.{clean}.{cnfPath} {x}");
							}
							else if ((timerMatch = s_C2dCompileTimePattern.Match(x)).Success) 
							{
								dnnfTime = ExtractC2dCompileTime(timerMatch);
								Console.Error.WriteLine($"{solver}.{clean}.{cnfPath} {x}");
							}
							else if ((timerMatch = s_DnnfTimePattern.Match(x)).Success)
							{
								totalTime = double.Parse(timerMatch.Groups["ms"].Value) / 1000;
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
						catch (Exception e) 
						{
							Console.Error.WriteLine($"{solver}.{clean}.{cnfPath} unknown error, {e}");
						}
						finally
						{
							await baselineTasksByFile[cnfPath];
							
							Utils.DataBuilder dataBuilder = new();
							// TODO: use string builder instead of full line
							dataBuilder.Append(solver);
							dataBuilder.Append(Path.GetFileName(cnfPath));
							dataBuilder.Append(clean == "clean" ? "Subsuming" : "All");
							dataBuilder.Append(finished? "Finished" : "Timeout");
							dataBuilder.Append(dnnfInfo?.Length);
							dataBuilder.Append(dnnfInfo?.Length * 1.0 / baselineNnfSize[cnfPath]);
							dataBuilder.Append(dtreeTime);
							dataBuilder.Append(dnnfTime + dtreeTime);
							dataBuilder.Append((dnnfTime + dtreeTime) / baselineCompileTime[cnfPath]);
							dataBuilder.Append(totalTime);
							dataBuilder.Append(totalTime / baselineTotalTime[cnfPath]);
							
							if (finished && myCount != baselineModelCounts[cnfPath]) 
							{
								dataBuilder.Append("incorrect");
							}
							
							Console.WriteLine(dataBuilder.ToString());
							
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
	static Regex s_DnnfTimePattern = new(@"\[timer\] completion: (?<ms>.+)");
	static Regex s_C2dTotalTimePattern = new(@"Total Time: (?<sec>.+)s");
	static Regex s_C2dCompileTimePattern = new(@"Compile Time: (?<ctime>.+)s / Pre-Processing: (?<pretime>.+)s / Post-Processing: (?<posttime>.+)s");

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
	
	private static Task RunBaseline(string cnfFile, Dictionary<string, string> baselineModelCounts, Dictionary<string, double> baselineTotalTime, Dictionary<string, double> baselineCompileTime, Dictionary<string, long> baselineNnfSize) 
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
				Match c2dTimeMatch = s_C2dTotalTimePattern.Match(c2dOutputLine);
				Match c2dCompileTimeMatch = s_C2dCompileTimePattern.Match(c2dOutputLine);
				if (c2dTimeMatch.Success)
				{
					baselineTotalTime[cnfFile] = double.Parse(c2dTimeMatch.Groups["sec"].Value);
				}
				else if (c2dCompileTimeMatch.Success) 
				{
					baselineCompileTime[cnfFile] = ExtractC2dCompileTime(c2dCompileTimeMatch);
				}
			}
		}

		c2dInstance.WaitForExit();
		if (File.Exists(cnfPath + ".nnf")) 
		{
			FileInfo dnnfInfo;
			dnnfInfo = new(cnfPath + ".nnf");
			baselineNnfSize[cnfFile] = dnnfInfo.Length;
			File.Delete(cnfPath + ".nnf");
		}
		
		Console.Error.WriteLine($"baseline finished: {cnfFile}");
	});
	
	private static double ExtractC2dCompileTime(Match match) 
	{
		double time = 0;
		time += double.Parse(match.Groups["ctime"].Value);
		time += double.Parse(match.Groups["pretime"].Value);
		time += double.Parse(match.Groups["posttime"].Value);
		return time;
	}
}
