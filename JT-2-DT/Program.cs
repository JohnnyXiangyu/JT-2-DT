using JT_2_DT;
using System.Diagnostics;

// TestJtreeImplementation();
// TestJtreeImplementationLong();
RunTestBench();
// TestMoralGraphImplementation();

void TestJtreeImplementationShort()
{
    Dtree dtree = new(Path.Combine("Examples", "short_jtree.txt"), new List<List<int>> { new() { 0, 1 }, new() { 2, 3 }, new() { 3, 4 } });
    foreach (var line in dtree.SerializeAsDtree())
    {
        Console.WriteLine(line());
    }
}

void TestJtreeImplementationLong()
{
    Dtree dtree = new(Path.Combine("Examples", "ex001.td"), new List<List<int>> { new() { 0, 1 }, new() { 2, 3 }, new() { 3, 4 } });
    foreach (var line in dtree.SerializeAsDtree())
    {
        Console.WriteLine(line());
    }
}

void TestMoralGraphImplementation()
{
    Cnf formula = new(Path.Combine("Examples", "short_cnf"));
    MoralGraph graph = new(formula);
    Console.WriteLine(graph.Serialize());
}

void RunTestBench()
{
    Cnf formula = new(Path.Combine("Examples", "sat-grid-pbl-0010.cnf"));
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
        Console.WriteLine(line());
    }

    // finally release the temp files
    File.Delete(tempTdFilename);
    File.Delete(tempTdFilename);
}