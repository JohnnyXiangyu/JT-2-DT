using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

namespace JT_2_DT.ExecutionModes;

public class CorrectnessBenchmark
{
	static Regex s_DtreeTimePattern = new(@"\[timer\] dtree: (?<ms>.+)");
	static Regex s_CompletionTimePattern = new(@"\[timer\] completion: (?<ms>.+)");
	
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
		Dictionary<string, int> baselineWidth = new();
		foreach (string file in cnfFiles)
		{
			baselineModelCounts[file] = "0";
			baselineTotalTime[file] = 0;
			baselineCompileTime[file] = 0;
			baselineNnfSize[file] = 0;
			baselineWidth[file] = 0;
		}

		// get baseline
		Dictionary<string, Task> baselineTasksByFile = new();
		foreach (string cnfFile in cnfFiles)
		{
			baselineTasksByFile[cnfFile] = RunBaseline(cnfFile, baselineModelCounts, baselineTotalTime, baselineCompileTime, baselineNnfSize, baselineWidth);
		}

		// csv header		
		StringBuilder csvHeader = new();
		csvHeader.Append("Solver, ");
		csvHeader.Append("CNF, ");
		csvHeader.Append("NNF Size Ratio to Vanilla, ");
		csvHeader.Append("DNNF Compilation Time Ratio to Vanilla, ");
		csvHeader.Append("Total Time Ratio to Vanilla, ");
		csvHeader.Append("Dtree Width, ");
		csvHeader.Append("Dtree width vanilla, ");
		csvHeader.Append("Dtree Mode, ");
		csvHeader.Append("Completion, ");
		csvHeader.Append("NNF Size, ");
		csvHeader.Append("NNF Size Vanilla, ");
		csvHeader.Append("Dtree Compilation Time, ");
		csvHeader.Append("DNNF Compilation Time, ");
		csvHeader.Append("DNNF Compilation Time Vanilla, ");
		csvHeader.Append("Total Time, ");
		csvHeader.Append("Total Time Vanilla, ");
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
						double dtreeTime = 0;
						double completionTime = 0;
						Utils.C2dLogInterpreter interpreter = new();
												
						// configure logger
						Logger logger = new((x) =>
						{							
							Match match;
							bool shouldLog = false;
							
							if ((match = s_DtreeTimePattern.Match(x)).Success) 
							{
								dtreeTime = double.Parse(match.Groups["ms"].Value) / 1000;
							}
							else if ((match = s_CompletionTimePattern.Match(x)).Success) 
							{
								completionTime = double.Parse(match.Groups["ms"].Value) / 1000;
							}
							else if (x.Length > 0 && x[0] != '[') 
							{
								shouldLog = interpreter.ProcessLog(x);
							}
							
							if (match.Success || shouldLog) 
							{
								Console.Error.WriteLine($"{solver}.{clean}.{Path.GetFileName(cnfPath)} {x}");
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
							throw;
						}
						finally
						{
							await baselineTasksByFile[cnfPath];
							
							Utils.DataBuilder dataBuilder = new();
							dataBuilder.Append(solver);
							dataBuilder.Append(Path.GetFileName(cnfPath));
							dataBuilder.Append(dnnfInfo?.Length * 1.0 / baselineNnfSize[cnfPath]);
							dataBuilder.Append(interpreter.CompileTime / baselineCompileTime[cnfPath]);
							dataBuilder.Append(completionTime / baselineTotalTime[cnfPath]);
							dataBuilder.Append(interpreter.Width);
							dataBuilder.Append(baselineWidth[cnfPath]);
							dataBuilder.Append(clean == "clean" ? "Subsuming" : "All");
							dataBuilder.Append(finished? "Finished" : "Timeout");
							dataBuilder.Append(dnnfInfo?.Length);
							dataBuilder.Append(baselineNnfSize[cnfPath]);
							dataBuilder.Append(dtreeTime);
							dataBuilder.Append(interpreter.CompileTime);
							dataBuilder.Append(baselineCompileTime[cnfPath]);
							dataBuilder.Append(completionTime);
							dataBuilder.Append(baselineTotalTime[cnfPath]);
							
							if (finished && interpreter.ModelCount != baselineModelCounts[cnfPath]) 
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
	
	private static Task RunBaseline(
		string cnfFile, Dictionary<string, string> baselineModelCounts, 
		Dictionary<string, double> baselineTotalTime, 
		Dictionary<string, double> baselineCompileTime, 
		Dictionary<string, long> baselineNnfSize,
		Dictionary<string, int> baselineWidth) 
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
		
		Utils.C2dLogInterpreter interpreter = new();

		string? c2dOutputLine = string.Empty;
		while ((c2dOutputLine = c2dInstance.StandardOutput.ReadLine()) != null)
		{
			_ = interpreter.ProcessLog(c2dOutputLine);
		}

		c2dInstance.WaitForExit();
		
		baselineModelCounts[cnfFile] = interpreter.ModelCount;
		baselineCompileTime[cnfFile] = interpreter.CompileTime;
		baselineTotalTime[cnfFile] = interpreter.TotalTime;
		baselineWidth[cnfFile] = interpreter.Width;
		
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
