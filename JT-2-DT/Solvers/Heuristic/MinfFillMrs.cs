using System.Diagnostics;

namespace JT_2_DT.Solvers.Heuristic;

public class MinFillMrs : SelfTerminatingHeuristicSolver
{
	protected override Process GetSolver()
	{
		Process solver = new();
		solver.StartInfo.FileName = Path.Combine("external_executables", "heuristic_solvers", $"minfill_mrs_{Defines.OsSuffix}");
		return solver;
	}
}
