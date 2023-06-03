using System.Diagnostics;

namespace JT_2_DT.Solvers.Heuristic;

public class Tamaki2017 : SigtermHeuristicSolver
{
    protected override Process GetSolver()
    {
        Process solver = new();
		solver.StartInfo.FileName = "java";
		solver.StartInfo.Arguments = $"-jar {Path.Combine("external_executables", "heuristic_solvers", "tamaki2017_heuristic.jar")}";
		return solver;
    }
}
