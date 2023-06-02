using System.Diagnostics;

namespace JT_2_DT.Solvers.Exact;

public class Twalgor : ITwSolver
{
	private static string _solverPath = Path.Combine("external_executables", "exact_solvers", "tw.jar");

	public void Execute(string inputPath, string outputPath)
	{
		using Process twSolver = new();
		twSolver.StartInfo.FileName = "java";
		twSolver.StartInfo.Arguments = $"-jar {_solverPath} {inputPath} {outputPath}";
		twSolver.Start();
		
		twSolver.WaitForExit(Defines.ExactSolverTimeout);

		if (!twSolver.HasExited) 
		{
			twSolver.Kill();
			throw new TimeoutException($"exact solver time out after {Defines.ExactSolverTimeout} ms!");
		}
	}
}
