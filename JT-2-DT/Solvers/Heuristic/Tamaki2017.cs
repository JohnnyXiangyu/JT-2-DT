using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JT_2_DT.Solvers.Heuristic;

public class Tamaki2017 : LinuxSolver
{
	const int s_Duration = 1000;
	
	public override void Execute(string inputPath, string outputPath)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			throw new NotImplementedException("flow cutter integration doesn't work on windows yet");
		}
		
		using Process solver = new();
		solver.StartInfo.FileName = "java";
		solver.StartInfo.Arguments = $"-jar {Path.Combine("external_executables", "heuristic_solvers", "tamaki2017_heuristic.jar")}";
		solver.StartInfo.RedirectStandardOutput = true;
		solver.StartInfo.RedirectStandardInput = true;
		solver.Start();
		
		// feed the file into stdin
		using (FileStream fs = File.OpenRead(inputPath)) 
		{
			fs.CopyTo(solver.StandardInput.BaseStream);
		}
		
		Task.Delay(s_Duration).Wait();
		
		Kill(solver);
		solver.WaitForExit();
		
		// read the output
		string output = solver.StandardOutput.ReadToEnd();
		File.WriteAllText(outputPath, output);
	}
}
