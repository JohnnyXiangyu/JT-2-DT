using System.Diagnostics;
using System.Reflection;

namespace JT_2_DT.Solvers.Heuristic;

public class FlowCutter : ITwSolver
{
    private const int TotalDuration = 1000;

    private static Process StartSolver(string inputPath)
    {
        string solverPath = Path.Combine("external_executables", "heuristic_solvers", "flow_cutter_pace17.exe");
        
        Process wrapper = new();
        wrapper.StartInfo.FileName = solverPath;
        wrapper.StartInfo.Arguments = $"{inputPath}";
        wrapper.StartInfo.RedirectStandardOutput = true;
        return wrapper;
    }

    private static Process StartKiller(Process victim)
    {
        string killerPath = Path.Combine("external_executables", "windows-kill.exe");

        Process killerInstance = new();
        killerInstance.StartInfo.FileName = killerPath;
        killerInstance.StartInfo.Arguments = $"-SIGINT {victim.Id}";
        return killerInstance;
    }

    public void Execute(string inputPath, string outputPath)
    {
        using Process solver = StartSolver(inputPath);
        solver.Start();

        Task.Delay(TotalDuration).Wait();
        using Process killer = StartKiller(solver);

        ControlHandling.DisableSigint();
        killer.Start();
        killer.WaitForExit();
        solver.WaitForExit();
        ControlHandling.EnableSigint();

        // read the output
        string output = solver.StandardOutput.ReadToEnd();
        File.WriteAllText(outputPath, output);

    }
}
