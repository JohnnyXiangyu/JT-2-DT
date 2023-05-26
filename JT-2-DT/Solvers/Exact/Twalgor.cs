using System.Diagnostics;

namespace JT_2_DT.Solvers.Exact;

public class Twalgor : ITwSolver
{
    public void Execute(string inputPath, string outputPath)
    {
        using Process twSolver = new();
        string twPath = Path.Combine("external_executables", "tw.jar");
        twSolver.StartInfo.FileName = "java";
        twSolver.StartInfo.Arguments = $"-jar {twPath} {inputPath} {outputPath}";
        twSolver.Start();
        twSolver.WaitForExit();
    }

    public Task ExecuteAsync(string inputPath, string outputPath)
    {
        Process twSolver = new();
        string twPath = Path.Combine("external_executables", "tw.jar");
        twSolver.StartInfo.FileName = "java";
        twSolver.StartInfo.Arguments = $"-jar {twPath} {inputPath} {outputPath}";
        twSolver.Start();
        return Task.Run(async () =>
        {
            await twSolver.WaitForExitAsync();
            twSolver.Dispose();
        });
    }
}
