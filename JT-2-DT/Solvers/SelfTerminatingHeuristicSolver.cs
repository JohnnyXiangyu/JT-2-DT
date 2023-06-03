using System.Diagnostics;

namespace JT_2_DT.Solvers;

public abstract class SelfTerminatingHeuristicSolver : ITwSolver
{
	public void Execute(string inputPath, string outputPath) 
	{		
		using Process solver = GetSolver();
		solver.StartInfo.RedirectStandardInput = true;
		solver.StartInfo.RedirectStandardOutput = true;
		solver.Start();
		
		using (FileStream fs = File.OpenRead(inputPath)) 
		{
			fs.CopyTo(solver.StandardInput.BaseStream);
		}
		solver.StandardInput.Close();
		
		solver.WaitForExit(Defines.HeuristicSolverTimeout);
		
		if (!solver.HasExited) 
		{
			solver.Kill();
			throw new TimeoutException($"solver did not finish under {Defines.HeuristicSolverTimeout} ms");
		}

		// read the output
		string output = solver.StandardOutput.ReadToEnd();
		File.WriteAllText(outputPath, output);
	}
	
	protected abstract Process GetSolver();
}
