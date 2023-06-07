using System.Diagnostics;
using JT_2_DT.Utils;

namespace JT_2_DT.ExecutionModes;

public class FullPipeline
{
	public static void Run(string[] args, Logger logger)
	{
		Stopwatch sharedTimer = Stopwatch.StartNew();
		
		// requesting temp files
		using TempFileAgent tdFileAgent = new();
		using TempFileAgent grFileAgent = new();
		using TempFileAgent dtreeFileAgent = new();

		string tempTdFilename = tdFileAgent.TempFilePath;
		string tempGrFilename = grFileAgent.TempFilePath;
		string tempDtreeFilename = dtreeFileAgent.TempFilePath;

		// overall arguments
		string cnfPath = string.Empty;
		bool useCleanBuild = false;
		string mode = "--dnnf";
		string solverName = String.Empty;
		string dtFile = String.Empty;

		// load from input or commandline
		if (args.Length >= 1)
		{
			mode = args[0];
		}
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
			solverName = args[3];
		}
		if (args.Length >= 5) 
		{
			dtFile = args[4];
		}

		// moralization
		Cnf formula = new(cnfPath);
		logger.LogInformation($"[timer] cnf: {sharedTimer.Elapsed.TotalSeconds}");
		MoralGraph graph = new(formula);
		logger.LogInformation($"[timer] moral graph: {sharedTimer.Elapsed.TotalSeconds}");

		// if mode is moral graph, break here
		if (mode == "--moral-graph")
		{
			// Console.Write(graph.Serialize());
			return;
		}

		// serialize graph to temp file
		graph.OutputToFile(tempGrFilename);

		// load solver
		ITwSolver solver;
		switch (solverName) 
		{
		case "--tamaki2017-heuristic":
			solver = new JT_2_DT.Solvers.Heuristic.Tamaki2017();
			break;
		case "--tamaki2017-exact":
			solver = new JT_2_DT.Solvers.Exact.Tamaki2017();
			break;
		case "--flowcutter":
			solver = new JT_2_DT.Solvers.Heuristic.FlowCutter();
			break;
		case "--htd":
			solver = new JT_2_DT.Solvers.Heuristic.Htd();
			break;
		case "--tdlib-exact":
			solver = new JT_2_DT.Solvers.Exact.Tdlib();
			break;
		default:
			solver = new JT_2_DT.Solvers.Heuristic.Tamaki2017();
			break;
		}
		
		try 
		{
			solver.Execute(tempGrFilename, tempTdFilename);
		}
		catch (TimeoutException) 
		{
			logger.LogInformation("[solver] timeout!");
			return;
		}
		logger.LogInformation($"[timer] solver-finish: {sharedTimer.Elapsed.TotalSeconds}");
		
		// dtree compilation
		Dtree dtree = new(tempTdFilename, formula.Clauses, useCleanBuild);
		logger.LogInformation($"[timer] dtree-pregen: {sharedTimer.Elapsed.TotalSeconds}");

		// if we only want dtree
		if (mode == "--dtree")
		{
			if (dtFile != string.Empty) 
			{
				File.WriteAllLines(dtFile, dtree.SerializeAsDtree());
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
		else 
		{
			File.WriteAllLines(tempDtreeFilename, dtree.SerializeAsDtree());
			logger.LogInformation($"[timer] dtree: {sharedTimer.Elapsed.TotalSeconds}");
		}

		// c2d invocation
		string c2dPath = Path.Combine("external_executables", $"c2d_{Defines.OsSuffix}");
		using (Process c2dInstance = new())
		{
			c2dInstance.StartInfo.FileName = c2dPath;
			c2dInstance.StartInfo.Arguments = $"-in {cnfPath} -dt_in {tempDtreeFilename} -count -smooth_all -reduce";
			c2dInstance.StartInfo.RedirectStandardOutput = true;
			c2dInstance.Start();
			
			Task readerTask = Task.Run(() => 
			{
				string? c2dOutputLine;
				while ((c2dOutputLine = c2dInstance.StandardOutput.ReadLine()) != null) 
				{
					logger.LogInformation(c2dOutputLine);
				}
			});
			
			if (!c2dInstance.WaitForExit(Defines.C2dTimeout)) 
			{
				c2dInstance.Kill();
				throw new TimeoutException($"c2d didn't finish within {Defines.C2dTimeout} ms");
			}
			
			readerTask.Wait();
		}
		logger.LogInformation($"[timer] completion: {sharedTimer.Elapsed.TotalSeconds}");
	}
}
