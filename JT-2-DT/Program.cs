using JT_2_DT;
using System.Diagnostics;

Console.WriteLine("hello world");

Stopwatch timer = Stopwatch.StartNew();

Cnf formula = new(Path.Combine("Examples", "sat-grid-pbl-0015.cnf"));
MoralGraph graph = new(formula);

string tempGrFilename = Path.GetTempFileName();
graph.OutputToFile(tempGrFilename);

// compute the tree decomposition
string tempTdFilename = Path.GetTempFileName();
Process twSolver = new();
twSolver.StartInfo.FileName = "java";
twSolver.StartInfo.Arguments = $"-jar {Path.Combine("external_executables", "tw.jar")} {tempGrFilename} {tempTdFilename}";
twSolver.Start();
twSolver.WaitForExit();

// compute the dtree
Dtree dtree = new(tempTdFilename, formula.Clauses);
foreach (var line in dtree.SerializeAsDtree())
{
    Trace.WriteLine(line);
}

// report time
Trace.WriteLine($"{timer.Elapsed.TotalSeconds} seconds");

// finally release the temp files
File.Delete(tempTdFilename);
File.Delete(tempTdFilename);