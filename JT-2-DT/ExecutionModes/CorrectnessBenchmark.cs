using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

namespace JT_2_DT.ExecutionModes;

public class CorrectnessBenchmark
{
	static Regex s_DtreeTimePattern = new(@"\[timer\] dtree: (?<ms>.+)");
	static Regex s_CompletionTimePattern = new(@"\[timer\] completion: (?<ms>.+)");
	// const string Mode = "--dnnf";

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
		csvHeader.Append("Dtree Width, ");
		csvHeader.Append("Dtree Mode, ");
		csvHeader.Append("Completion, ");
		csvHeader.Append("NNF Size, ");
		csvHeader.Append("Dtree Compilation Time, ");
		csvHeader.Append("DNNF Compilation Time, ");
		csvHeader.Append("Total Time, ");
		csvHeader.Append("Model Count, ");
		Console.WriteLine(csvHeader.ToString());

		string[] solvers =
		{
			"tamaki2017-heuristic",
			"flowcutter",
			// "htd",
			// "tamaki2017-exact",
			// "tdlib-exact",
		};

		string[] cleanness =
		{
			"clean",
			"dirty"
		};
		
		Utils.BatchGenerator batcher = new(_benchMarkFolders, Defines.InstanceLimit);
	
		foreach (var cnfFiles in batcher.LoadBenchmarks())
		{
			Console.Error.WriteLine("next benchmarks:");
			foreach (string file in cnfFiles)
			{
				Console.Error.Write("    ");
				Console.Error.WriteLine(file);
			}
			Console.Error.WriteLine("");

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
			
			if (!dtreeGenerator.WaitForExit(Defines.DtreeTimeout)) 
			{
				dtreeGenerator.Kill(true);
				throw new TimeoutException($"dtree generation timeout after {Defines.DtreeTimeout}");
			}
			
			await selfReaderTask;
			
			if (solverTimeout) 
			{
				throw new TimeoutException($"tree width solver timeout after {Defines.HeuristicSolverTimeout}");
			}
			
			Log("end dtree");
			
			// start c2d
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
				
				if (!c2dInstance.WaitForExit(Defines.C2dTimeout)) 
				{
					c2dInstance.Kill();
					throw new TimeoutException($"c2d didn't finish within remaining {Defines.C2dTimeout} ms");
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
			Utils.DataBuilder dataBuilder = new();
			dataBuilder.Append(solver);
			dataBuilder.Append(Path.GetFileName(cnfPath));
			dataBuilder.Append(interpreter.Width);
			dataBuilder.Append(clean == "clean" ? "Subsuming" : "All");
			dataBuilder.Append(finished ? "Finished" : "Timeout");
			dataBuilder.Append(dnnfInfo?.Length);
			dataBuilder.Append(dtreeTime);
			dataBuilder.Append(interpreter.CompileTime);
			dataBuilder.Append(completionTime);
			dataBuilder.Append(interpreter.ModelCount);

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
