using System.Diagnostics;

namespace JT_2_DT.Solvers.Exact;

public class Tdlib : SuicidalSolver
{
	protected override Process GetSolver()
	{
		Process solver = new();
		solver.StartInfo.FileName = Path.Combine("external_executables", $"p17_{Defines.OsSuffix}", "grtd");
		solver.StartInfo.Arguments = "-m 64";
		return solver;
	}
}