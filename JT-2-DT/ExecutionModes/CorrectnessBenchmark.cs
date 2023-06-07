using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

namespace JT_2_DT.ExecutionModes;

public class CorrectnessBenchmark
{
	static Regex s_DtreeTimePattern = new(@"\[timer\] dtree: (?<ms>.+)");
	static Regex s_CompletionTimePattern = new(@"\[timer\] completion: (?<ms>.+)");
	// const string Mode = "--dnnf";

	Dictionary<string, string> _baselineModelCounts = new();
	Dictionary<string, double> _baselineTotalTime = new();
	Dictionary<string, double> _baselineCompileTime = new();
	Dictionary<string, long> _baselineNnfSize = new();
	Dictionary<string, int> _baselineWidth = new();
	Dictionary<string, Task> _baselineTasksByFile = new();
	Dictionary<string, bool> _baselineSuccess = new();

	IEnumerable<string> _benchMarkFolders;

	public CorrectnessBenchmark(IEnumerable<string> folders)
	{
		_benchMarkFolders = folders;
	}

	public void Run()
	{
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
		csvHeader.Append("Model Count, ");
		csvHeader.Append("Model Count vanilla, ");
		Console.WriteLine(csvHeader.ToString());

		string[] solvers =
		{
			//"tamaki2017-heuristic",
			//"flowcutter",
			// "htd",
			"tamaki2017-exact",
			// "tdlib-exact",
		};

		string[] cleanness =
		{
			"clean",
			"dirty"
		};

		foreach (var cnfFiles in LoadBenchmarks())
		{
			Console.Error.WriteLine("next benchmarks:");
			foreach (string file in cnfFiles)
			{
				Console.Error.Write("    ");
				Console.Error.WriteLine(file);
			}
			Console.Error.WriteLine("");

			// prepare baseline
			foreach (string file in cnfFiles)
			{
				_baselineModelCounts[file] = "0";
				_baselineTotalTime[file] = double.MaxValue;
				_baselineCompileTime[file] = double.MaxValue;
				_baselineNnfSize[file] = int.MaxValue;
				_baselineWidth[file] = 0;
				_baselineSuccess[file] = true;
			}

			// get baseline
			foreach (string cnfFile in cnfFiles)
			{
				_baselineTasksByFile[cnfFile] = RunBaseline(cnfFile);
			}

			// spawn instance tasks
			List<Task> instanceTasks = new();
			foreach (string solver in solvers)
			{
				foreach (string clean in cleanness)
				{
					foreach (string cnfPath in cnfFiles)
					{
						Task instanceTask = RunInstance(solver, clean, cnfPath);
						instanceTasks.Add(instanceTask);
					}
				}
			}

			Task.WaitAll(instanceTasks.ToArray());
			Console.Error.WriteLine("batch done \n");
		}

		Console.Error.WriteLine("done");
	}

	private IEnumerable<List<string>> LoadBenchmarks()
	{
		List<string> cnfFiles = new();
		foreach (string file in NextBenchmark())
		{
			if (cnfFiles.Count >= Defines.InstanceLimit)
			{
				yield return cnfFiles;
				cnfFiles = new();
			}

			cnfFiles.Add(file);
		}

		yield return cnfFiles;
	}

	private IEnumerable<string> NextBenchmark()
	{
		foreach (string folder in _benchMarkFolders)
		{
			DirectoryInfo d = new DirectoryInfo(Path.Combine("Examples", folder));
			FileInfo[] Files = d.GetFiles("*.cnf");

			foreach (FileInfo file in Files)
			{
				yield return file.FullName;
			}
		}
	}

	private Task RunBaseline(string cnfFile) => Task.Run(async ()=>
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
		Task readerTask = interpreter.Reader(c2dInstance);

		bool success = c2dInstance.WaitForExit(Defines.BaselineTimeout);
		if (!success)
		{
			c2dInstance.Kill();
			_baselineSuccess[cnfFile] = false;
		}
		
		await readerTask;

		_baselineModelCounts[cnfFile] = interpreter.ModelCount;
		_baselineCompileTime[cnfFile] = interpreter.CompileTime;
		_baselineTotalTime[cnfFile] = interpreter.TotalTime;
		_baselineWidth[cnfFile] = interpreter.Width;

		if (File.Exists(cnfPath + ".nnf"))
		{
			FileInfo dnnfInfo;
			dnnfInfo = new(cnfPath + ".nnf");
			_baselineNnfSize[cnfFile] = dnnfInfo.Length;
			File.Delete(cnfPath + ".nnf");
		}

		if (_baselineSuccess[cnfFile])
		{
			Console.Error.WriteLine($"baseline finished: {cnfFile}");
		}
		else
		{
			Console.Error.WriteLine($"benchmark failed: {cnfPath}");
		}
	});

	private Task RunInstance(string solver, string clean, string cnfPath)
		=> Task.Run(async () =>
	{
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
		
		// configure the logger
		double dtreeTime = double.MaxValue;
		double completionTime = double.MaxValue;
		Utils.C2dLogInterpreter interpreter = new();

		// run the main routine
		try
		{
			Stopwatch totalTimer = Stopwatch.StartNew();
			
			void Log(string message) 
			{
				Console.Error.WriteLine($"{solver}.{clean}.{cnfPath} {totalTimer.Elapsed.TotalSeconds} {message}");
			}
			
			// start a new dtree generator instance
			using Utils.TempFileAgent tempDtreeFile = new();
			using Process dtreeGenerator = new();
			dtreeGenerator.StartInfo.FileName = Path.Combine(Defines.ExeDirectory!, "JT-2-DT");
			dtreeGenerator.StartInfo.Arguments = $"--dtree {tempCnf.TempFilePath} --{clean} --{solver} {tempDtreeFile.TempFilePath}";
			dtreeGenerator.StartInfo.RedirectStandardError = true;
			dtreeGenerator.StartInfo.RedirectStandardOutput = true;
			dtreeGenerator.Start();
			
			Log("start dtree");
			
			bool solverTimeout = false;	
			
			Task selfReaderTask = Task.Run(async () => 
			{
				string? nextLine = null;
				while ((nextLine = await dtreeGenerator.StandardError.ReadLineAsync()) != null) 
				{
					if (nextLine == "[solver] timeout!") 
					{
						solverTimeout = true;
						return;
					}
					
					Match match;

					if ((match = s_DtreeTimePattern.Match(nextLine)).Success)
					{
						dtreeTime = double.Parse(match.Groups["ms"].Value);
					}

					if (match.Success)
					{
						Console.Error.WriteLine($"{solver}.{clean}.{Path.GetFileName(cnfPath)} {nextLine}");
					}
				}
			});
			
			if (!dtreeGenerator.WaitForExit(Defines.BaselineTimeout)) 
			{
				dtreeGenerator.Kill(true);
				throw new TimeoutException($"dtree generation timeout after {Defines.BaselineTimeout}");
			}
			
			await selfReaderTask;
			
			if (solverTimeout) 
			{
				throw new TimeoutException($"tree width solver timeout after {Defines.HeuristicSolverTimeout}");
			}
			
			Log("end dtree");
			
			// start c2d
			int renamingTime = Defines.BaselineTimeout - (int)Math.Floor(totalTimer.Elapsed.TotalSeconds);
			string c2dPath = Path.Combine("external_executables", $"c2d_{Defines.OsSuffix}");
			using (Process c2dInstance = new())
			{
				c2dInstance.StartInfo.FileName = c2dPath;
				c2dInstance.StartInfo.Arguments = $"-in {tempCnf.TempFilePath} -dt_in {tempDtreeFile.TempFilePath} -count -smooth_all -reduce";
				c2dInstance.StartInfo.RedirectStandardOutput = true;
				c2dInstance.Start();
				
				Log("start c2d");
					
				Task readerTask = Task.Run(() => 
				{
					string? c2dOutputLine;
					while ((c2dOutputLine = c2dInstance.StandardOutput.ReadLine()) != null) 
					{
						_ = interpreter.ProcessLog(c2dOutputLine);
					}
				});
				
				if (!c2dInstance.WaitForExit(renamingTime)) 
				{
					c2dInstance.Kill();
					throw new TimeoutException($"c2d didn't finish within remaining {renamingTime} ms");
				}
				
				readerTask.Wait();
				
				Log("end c2d");
			}
			
			// take time
			completionTime = totalTimer.Elapsed.TotalSeconds;

			// calculate dnnf size
			string dnnfPath = $"{tempCnf.TempFilePath}.nnf";
			if (File.Exists(dnnfPath))
				dnnfInfo = new(dnnfPath);
		}
		catch (Exception e)
		{
			finished = false;
			Console.Error.WriteLine($"{solver}.{clean}.{cnfPath} {e.Message}");
		}
		finally
		{
			await _baselineTasksByFile[cnfPath];

			Utils.DataBuilder dataBuilder = new();
			dataBuilder.Append(solver);
			dataBuilder.Append(Path.GetFileName(cnfPath));
			dataBuilder.Append(dnnfInfo?.Length * 1.0 / _baselineNnfSize[cnfPath]);
			dataBuilder.Append(interpreter.CompileTime / _baselineCompileTime[cnfPath]);
			dataBuilder.Append(completionTime / _baselineTotalTime[cnfPath]);
			dataBuilder.Append(interpreter.Width);
			dataBuilder.Append(_baselineWidth[cnfPath]);
			dataBuilder.Append(clean == "clean" ? "Subsuming" : "All");
			dataBuilder.Append(finished ? "Finished" : "Timeout");
			dataBuilder.Append(dnnfInfo?.Length);
			dataBuilder.Append(_baselineNnfSize[cnfPath]);
			dataBuilder.Append(dtreeTime);
			dataBuilder.Append(interpreter.CompileTime);
			dataBuilder.Append(_baselineCompileTime[cnfPath]);
			dataBuilder.Append(completionTime);
			dataBuilder.Append(_baselineTotalTime[cnfPath]);
			dataBuilder.Append(interpreter.ModelCount);
			dataBuilder.Append(_baselineModelCounts[cnfPath]);

			if (finished && (!_baselineSuccess[cnfPath] || _baselineModelCounts[cnfPath] == "undefined"))
			{
				dataBuilder.Append("baseline timeout");
			}
			else if (finished && interpreter.ModelCount != _baselineModelCounts[cnfPath])
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

	private double ExtractC2dCompileTime(Match match)
	{
		double time = 0;
		time += double.Parse(match.Groups["ctime"].Value);
		time += double.Parse(match.Groups["pretime"].Value);
		time += double.Parse(match.Groups["posttime"].Value);
		return time;
	}
}
