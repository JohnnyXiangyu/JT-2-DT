using System.Diagnostics;

namespace JT_2_DT.Solvers.Exact
{
	internal class Tamaki2017 : SuicidalSolver
	{
		// // TODO: figure out how to configure the command line arguments
		// public void Execute(string inputPath, string outputPath)
		// {
		// 	using Process solver = new();
		// 	solver.StartInfo.FileName = "java";
		// 	solver.StartInfo.Arguments = $"-jar {Path.Combine("external_executables", "exact_solvers", "tamaki2017_exact.jar")}";
		// 	solver.StartInfo.RedirectStandardOutput = true;
		// 	solver.StartInfo.RedirectStandardInput = true;
		// 	solver.Start();

		// 	using (FileStream fs = File.OpenRead(inputPath)) 
		// 	{
		// 		fs.CopyTo(solver.StandardInput.BaseStream);
		// 	}
		// 	solver.StandardInput.Close();

		// 	solver.WaitForExit();
		// 	string result = solver.StandardOutput.ReadToEnd();
		// 	File.WriteAllText(outputPath, result);
		// }
		protected override Process GetSolver()
		{
			Process solver = new();
			solver.StartInfo.FileName = "java";
			solver.StartInfo.Arguments = $"-jar {Path.Combine("external_executables", "exact_solvers", "tamaki2017_exact.jar")}";
			return solver;
		}
	}
}
