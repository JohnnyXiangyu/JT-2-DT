using System.Diagnostics;

namespace JT_2_DT.Solvers.Heuristic;

public class Htd : UnlimitedStdinSolver 
{
	protected override Process GetSolver() 
	{
		Process solver = new();
		solver.StartInfo.FileName = Path.Combine("external_executables", "heuristic_solvers", $"htd_{Defines.OsSuffix}");
		solver.StartInfo.Arguments = "--opt width";
		return solver;
	}
}
