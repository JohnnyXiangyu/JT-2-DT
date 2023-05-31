using System.Diagnostics;

namespace JT_2_DT.Solvers.Exact;

public class Tdlib : ITwSolver
{
	public void Execute(string inputPath, string outputPath)
	{
		using Process solver = new();
		solver.StartInfo.FileName = Path.Combine("external_executables", $"p17_{Defines.OsSuffix}", "grtd");
		solver.StartInfo.Arguments = "-m 64";
		solver.StartInfo.RedirectStandardInput = true;
		solver.StartInfo.RedirectStandardOutput = true;
		solver.Start();
		
		using (FileStream fs = File.OpenRead(inputPath)) 
		{
			fs.CopyTo(solver.StandardInput.BaseStream);
		}		
		solver.StandardInput.Close();
		
		solver.WaitForExit();
		
		string output = solver.StandardOutput.ReadToEnd();
		File.WriteAllText(outputPath, output);
	}
}