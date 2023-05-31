using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace JT_2_DT.Solvers.Heuristic;

public class FlowCutter : LinuxSolver
{
	private const int TotalDuration = 1000;

	private static Process StartSolver(string inputPath)
	{
		string solverPath = Path.Combine("external_executables", "heuristic_solvers", $"flow_cutter_pace17_{Defines.OsSuffix}");
		
		Process wrapper = new();
		wrapper.StartInfo.FileName = solverPath;
		wrapper.StartInfo.Arguments = $"{inputPath}";
		wrapper.StartInfo.RedirectStandardOutput = true;
		return wrapper;
	}

	public override void Execute(string inputPath, string outputPath)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			throw new NotImplementedException("flow cutter integration doesn't work on windows yet");
		}
		
		using Process solver = StartSolver(inputPath);
		solver.Start();

		Task.Delay(TotalDuration).Wait();
		
		Kill(solver);
		solver.WaitForExit();

		// read the output
		string output = solver.StandardOutput.ReadToEnd();
		File.WriteAllText(outputPath, output);

	}
}
