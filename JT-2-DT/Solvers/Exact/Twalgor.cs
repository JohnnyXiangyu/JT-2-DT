using System.Diagnostics;

namespace JT_2_DT.Solvers.Exact;

public class Twalgor : ITwSolver
{
    private static string _solverPath = Path.Combine("external_executables", "exact_solvers", "tw.jar");

    public void Execute(string inputPath, string outputPath)
    {
        using Process twSolver = new();
        twSolver.StartInfo.FileName = "java";
        twSolver.StartInfo.Arguments = $"-jar {_solverPath} {inputPath} {outputPath}";
        twSolver.Start();
        twSolver.WaitForExit();
    }

    // public Task ExecuteAsync(string inputPath, string outputPath)
    // {
    //     Process twSolver = new();
    //     twSolver.StartInfo.FileName = "java";
    //     twSolver.StartInfo.Arguments = $"-jar {_solverPath} {inputPath} {outputPath}";
    //     twSolver.Start();
    //     return Task.Run(async () =>
    //     {
    //         await twSolver.WaitForExitAsync();
    //         twSolver.Dispose();
    //     });
    // }
}
