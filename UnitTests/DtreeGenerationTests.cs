using JT_2_DT;
using JT_2_DT.Solvers.Exact;
using System.Diagnostics;

namespace UnitTests;

[TestClass]
public class DtreeGenerationTests
{
    private static void DtreeGenerationBench(string input, bool useCleanBuild)
    {
        Cnf formula = new(input);
        MoralGraph graph = new(formula);

        string tempGrFilename = Path.GetTempFileName();
        graph.OutputToFile(tempGrFilename);

        // compute the tree decomposition
        string tempTdFilename = Path.GetTempFileName();
        ITwSolver solver = new Twalgor();
        solver.Execute(tempGrFilename, tempTdFilename);

        // compute the dtree
        Dtree dtree = new(tempTdFilename, formula.Clauses, useCleanBuild);
        foreach (var line in dtree.SerializeAsDtree())
        {
            Console.WriteLine(line);
        }

        // finally release the temp files
        File.Delete(tempGrFilename);
        File.Delete(tempTdFilename);
    }

    [TestMethod]
    public void MiniBenchDirty()
    {
        DtreeGenerationBench(Path.Combine("Examples", "short_cnf"), false);
    }

    [TestMethod]
    public void MiniBenchClean()
    {
        DtreeGenerationBench(Path.Combine("Examples", "short_cnf"), true);
    }

    [TestMethod]
    public void StandardBenchDirty()
    {
        DtreeGenerationBench(Path.Combine("Examples", "sat-grid-pbl-0010.cnf"), false);
    }

    [TestMethod]
    public void StandardBenchClean()
    {
        DtreeGenerationBench(Path.Combine("Examples", "sat-grid-pbl-0010.cnf"), true);
    }
}
