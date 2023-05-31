using System.Diagnostics;

namespace JT_2_DT.Solvers;

public abstract class LinuxSolver : ITwSolver
{
	public abstract void Execute(string inputPath, string outputPath);
	
	protected Task Kill(Process victim) 
	{
		string killerPath = "kill";

		Process killerInstance = new();
		killerInstance.StartInfo.FileName = killerPath;
		killerInstance.StartInfo.Arguments = $"-TERM {victim.Id}";
		killerInstance.Start();
		return killerInstance.WaitForExitAsync();
	}
}