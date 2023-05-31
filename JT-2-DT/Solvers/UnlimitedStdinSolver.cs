using System.Diagnostics;

namespace JT_2_DT.Solvers.Heuristic;

public abstract class UnlimitedStdinSolver : ITwSolver
{
	public void Execute(string inputPath, string outputPath) 
	{		
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
		
		solver.WaitForExit();

		// read the output
		string output = solver.StandardOutput.ReadToEnd();
		File.WriteAllText(outputPath, output);
	}
	
	protected abstract Process GetSolver();
}
