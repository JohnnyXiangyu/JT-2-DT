using System.Diagnostics;

namespace JT_2_DT.Solvers.Exact;

public abstract class SuicidalSolver : ITwSolver
{
	private static string _solverPath = Path.Combine("external_executables", "exact_solvers", "tw.jar");

	public void Execute(string inputPath, string outputPath)
	{		
		using Process solver = GetSolver();
		solver.StartInfo.RedirectStandardOutput = true;
		solver.StartInfo.RedirectStandardInput = true;
		solver.Start();

		using (FileStream fs = File.OpenRead(inputPath))
		{
			fs.CopyTo(solver.StandardInput.BaseStream);
		}
		solver.StandardInput.Close();

		solver.WaitForExit(Defines.ExactSolverTimeout);
		if (!solver.HasExited) 
		{
			solver.Kill();
			throw new TimeoutException($"exact solver time out after {Defines.ExactSolverTimeout} ms!");
		}
		
		string result = solver.StandardOutput.ReadToEnd();
		File.WriteAllText(outputPath, result);
	}
	
	protected abstract Process GetSolver();
}
