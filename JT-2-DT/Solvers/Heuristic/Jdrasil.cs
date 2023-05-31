using System.Diagnostics;

namespace JT_2_DT.Solvers.Heuristic;

public class Jdrasil : TimeBoundLinuxSolver
{
	protected override Process GetSolver()
	{
		Process solver = new();
		solver.StartInfo.FileName = "bash";
		solver.StartInfo.Arguments = Path.Combine("external_executables", "Jdrasil", "build", "tw-heuristic");
		return solver;
	}
}
