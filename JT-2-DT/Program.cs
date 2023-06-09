﻿using JT_2_DT;
using JT_2_DT.Solvers.Exact;
using JT_2_DT.Solvers.Heuristic;
using System.Diagnostics;

// requesting temp files
string tempTdFilename = Path.GetTempFileName();
string tempGrFilename = Path.GetTempFileName();
string tempDtreeFilename = Path.GetTempFileName();

// some external file path definitions
string c2dPath = Path.Combine("external_executables", "c2d_windows.exe");

// overall arguments
string cnfPath = string.Empty;
bool useCleanBuild = false;

// load from input or commandline
if (args.Length <= 0)
{
    Console.WriteLine("example cnf file: ");
    cnfPath = Path.Combine("Examples", Console.ReadLine()!);
    Console.WriteLine("use clean build? [Y/n] ");
    string? answer = Console.ReadLine();
    useCleanBuild = answer == null || (answer != "n" && answer != "N");
}
else
{
    if (args.Length >= 1)
    {
        cnfPath = args[0];
    }
    if (args.Length == 2)
    {
        useCleanBuild = args[1] == "--clean";
    }
    if (args.Length > 2)
    {
        Console.WriteLine("at most 2 arguments are accepted");
        return;
    }
}

Stopwatch timer = Stopwatch.StartNew();

// moralization
Cnf formula = new(cnfPath);
MoralGraph graph = new(formula);
graph.OutputToFile(tempGrFilename);

// solver
ITwSolver solver = new FlowCutter();
solver.Execute(tempGrFilename, tempTdFilename);
// await solver.ExecuteAsync(tempGrFilename, tempTdFilename);

// dtree compilation
Dtree dtree = new(tempTdFilename, formula.Clauses, useCleanBuild);
File.WriteAllLines(tempDtreeFilename, dtree.SerializeAsDtree());

// c2d invocation
using (Process c2dInstance = new())
{
    c2dInstance.StartInfo.FileName = c2dPath;
    c2dInstance.StartInfo.Arguments = $"-in {cnfPath} -dt_in {tempDtreeFilename}";
    c2dInstance.Start();
    c2dInstance.WaitForExit();
}

// report time
Trace.WriteLine($"{timer.Elapsed.TotalSeconds} seconds");

// finally release the temp files
File.Delete(tempTdFilename);
File.Delete(tempGrFilename);
File.Delete(tempDtreeFilename);
