using System.Diagnostics;

namespace JT_2_DT.Solvers.Heuristic;

public class FlowCutter : TimeBoundLinuxSolver
{
	private const int TotalDuration = 1000;

	protected override Process GetSolver() 
	{
		string solverPath = Path.Combine("external_executables", "heuristic_solvers", $"flow_cutter_pace17_{Defines.OsSuffix}");
		
		Process solver = new();
		solver.StartInfo.FileName = solverPath;
		return solver;
	}
}
