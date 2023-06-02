using System.Diagnostics;

namespace JT_2_DT.Solvers.Exact;

public class Tamaki2016 : SuicidalSolver
{
    protected override Process GetSolver()
	{
		Process solver = new();
		solver.StartInfo.FileName = Path.Combine("external_executables", "exact_solvers", $"tw-exact_{Defines.OsSuffix}");
		return solver;
	}
}