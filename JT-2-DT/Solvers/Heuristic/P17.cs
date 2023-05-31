using System.Diagnostics;

namespace JT_2_DT.Solvers.Heuristic;

public class P17 : LinuxSolver
{
	protected override Process GetSolver()
	{
		Process solver = new();
		solver.StartInfo.FileName = Path.Combine("external_executables", $"p17_{Defines.OsSuffix}", "grtd");
		solver.StartInfo.Arguments = "-m 128";
		return solver;
	}
}