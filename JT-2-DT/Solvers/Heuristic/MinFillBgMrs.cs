using System.Diagnostics;

namespace JT_2_DT.Solvers.Heuristic;

public class MinFillBgMrs : UnlimitedStdinSolver
{
	protected override Process GetSolver()
	{
		Process solver = new();
		solver.StartInfo.FileName = Path.Combine("external_executables", "heuristic_solvers", $"minfillbg_mrs_{Defines.OsSuffix}");
		return solver;
	}
}
