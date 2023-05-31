using System.Diagnostics;

namespace JT_2_DT.Solvers.Exact;

public class Jdrasil : ITwSolver
{
    public void Execute(string inputPath, string outputPath)
    {
        using Process solver = new();
        solver.StartInfo.FileName = "bash";
        solver.StartInfo.Arguments = Path.Combine("external_executables", "Jdrasil", "build", "tw-exact");

        solver.StartInfo.RedirectStandardOutput = true;
        solver.StartInfo.RedirectStandardInput = true;
        solver.Start();

        using (FileStream fs = File.OpenRead(inputPath))
        {
            fs.CopyTo(solver.StandardInput.BaseStream);
        }
        solver.StandardInput.Close();

        solver.WaitForExit();
        string result = solver.StandardOutput.ReadToEnd();
        File.WriteAllText(outputPath, result);
    }
}