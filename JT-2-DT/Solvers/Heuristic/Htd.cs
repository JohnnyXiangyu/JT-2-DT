using System.Diagnostics;

namespace JT_2_DT.Solvers.Heuristic;

public class Htd : SigtermHeuristicSolver 
{
	protected override Process GetSolver() 
	{
		Process solver = new();
		solver.StartInfo.FileName = Path.Combine("external_executables", "heuristic_solvers", $"htd_{Defines.OsSuffix}");
		solver.StartInfo.Arguments = "--opt width --iterations 0";
		return solver;
	}
}
