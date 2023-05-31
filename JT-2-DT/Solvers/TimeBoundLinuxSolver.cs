using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JT_2_DT.Solvers;

public abstract class TimeBoundLinuxSolver : ITwSolver
{	
	public void Execute(string inputPath, string outputPath) 
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			throw new NotImplementedException("flow cutter integration doesn't work on windows yet");
		}
		
		using Process solver = GetSolver();
		solver.StartInfo.RedirectStandardInput = true;
		solver.StartInfo.RedirectStandardOutput = true;
		solver.Start();
		
		using (FileStream fs = File.OpenRead(inputPath)) 
		{
			fs.CopyTo(solver.StandardInput.BaseStream);
		}
		solver.StandardInput.Close();

		Task.Delay(Defines.TotalDuration).Wait();
		
		Kill(solver);
		solver.WaitForExit();

		// read the output
		string output = solver.StandardOutput.ReadToEnd();
		File.WriteAllText(outputPath, output);
	}
	
	protected abstract Process GetSolver();
	
	protected void Kill(Process victim) 
	{
		string killerPath = "kill";

		using Process killerInstance = new();
		killerInstance.StartInfo.FileName = killerPath;
		killerInstance.StartInfo.Arguments = $"-TERM {victim.Id}";
		killerInstance.Start();
		killerInstance.WaitForExit();
	}
}