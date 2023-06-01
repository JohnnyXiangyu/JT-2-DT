using System.Diagnostics;

namespace JT_2_DT.ExecutionModes;

public class FullPipeline
{
	public static void Run(string[] args)
	{
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

		// load from input or commandline
		if (args.Length >= 1)
		{
			mode = args[0];
		}
		if (args.Length >= 2)
		{
			cnfPath = args[1];
		}
		if (args.Length == 3)
		{
			useCleanBuild = args[2] == "--clean";
		}
		if (args.Length == 4) 
		{
			solverName = args[3];
		}
		
		using TimerAgent timer = new();

		// moralization
		Cnf formula = new(cnfPath);
		MoralGraph graph = new(formula);

		// if mode is moral graph, break here
		if (mode == "--moral-graph")
		{
			Console.WriteLine(graph.Serialize());
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
		default:
			solver = new JT_2_DT.Solvers.Heuristic.Tamaki2017();
			break;
		}
		solver.Execute(tempGrFilename, tempTdFilename);

		// dtree compilation
		Dtree dtree = new(tempTdFilename, formula.Clauses, useCleanBuild);

		// if we only want dtree
		if (mode == "--dtree")
		{
			foreach (string line in dtree.SerializeAsDtree())
			{
				Console.WriteLine(line);
			}
			return;
		}

		// c2d invocation
		File.WriteAllLines(tempDtreeFilename, dtree.SerializeAsDtree());
		string c2dPath = Path.Combine("external_executables", $"c2d_{Defines.OsSuffix}");
		using (Process c2dInstance = new())
		{
			Console.WriteLine(File.Exists(c2dPath));

			c2dInstance.StartInfo.UseShellExecute = false;
			c2dInstance.StartInfo.FileName = c2dPath;
			c2dInstance.StartInfo.Arguments = $"-in {cnfPath} -dt_in {tempDtreeFilename} -count";
			c2dInstance.Start();
			c2dInstance.WaitForExit();
		}
	}
}