﻿using JT_2_DT;
using System.Diagnostics;

namespace UnitTests;

[TestClass]
public class BenchTests
{
    [TestMethod]
    public void RunTestBench()
    {
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
            Console.WriteLine(line());
        }

        // report time
        Console.WriteLine($"{timer.Elapsed.TotalSeconds} seconds");

        // finally release the temp files
        File.Delete(tempTdFilename);
        File.Delete(tempTdFilename);
    }

    [TestMethod]
    public void BenchmarkAll()
    {
        string[] suffices = new string[] { "0010", "0015", "0020", "0025", "0030" };
        IEnumerable<string> testFiles = suffices.Select(s => $"sat-grid-pbl-{s}.cnf");

        foreach (string file in testFiles)
        {
            Stopwatch timer = Stopwatch.StartNew();

            Console.WriteLine(file);

            Cnf formula = new(Path.Combine("Examples", file));
            MoralGraph graph = new(formula);

            string tempGrFilename = Path.GetTempFileName();
            graph.OutputToFile(tempGrFilename);

            Console.WriteLine($"{timer.Elapsed.TotalSeconds} moralization done");

            // compute the tree decomposition
            string tempTdFilename = Path.GetTempFileName();
            using Process twSolver = new();
            twSolver.StartInfo.FileName = "java";
            twSolver.StartInfo.Arguments = $"-jar {Path.Combine("external_executables", "tw.jar")} {tempGrFilename} {tempTdFilename}";
            twSolver.Start();
            twSolver.WaitForExit();

            Console.WriteLine($"{timer.Elapsed.TotalSeconds} solver exit");

            // compute the dtree
            Dtree dtree = new(tempTdFilename, formula.Clauses);

            // report time
            Console.WriteLine($"{timer.Elapsed.TotalSeconds} dtree created (excluding serialization)");

            // finally release the temp files
            File.Delete(tempTdFilename);
            File.Delete(tempTdFilename);
        }
    }

    [TestMethod]
    public void BenchmarkAllWithAscd()
    {
        string[] suffices = new string[] { "0010", "0015", "0020", "0025", "0030" };
        IEnumerable<string> testFiles = suffices.Select(s => $"sat-grid-pbl-{s}.cnf");

        foreach (string file in testFiles)
        {
            Stopwatch timer = Stopwatch.StartNew();

            Console.WriteLine(file);

            Cnf formula = new(Path.Combine("Examples", file));
            MoralGraph graph = new(formula);

            string tempGrFilename = Path.GetTempFileName();
            graph.OutputToFile(tempGrFilename);

            Console.WriteLine($"{timer.Elapsed.TotalSeconds} moralization done");

            // compute the tree decomposition
            string tempTdFilename = Path.GetTempFileName();
            using Process twSolver = new();
            twSolver.StartInfo.FileName = "java";
            twSolver.StartInfo.Arguments = $"-jar {Path.Combine("external_executables", "tw_acsd.jar")} -MMD {tempGrFilename} {tempTdFilename}";
            twSolver.Start();
            twSolver.WaitForExit();

            Console.WriteLine($"{timer.Elapsed.TotalSeconds} solver exit");

            // compute the dtree
            Dtree dtree = new(tempTdFilename, formula.Clauses);

            // report time
            Console.WriteLine($"{timer.Elapsed.TotalSeconds} dtree created (excluding serialization)");

            // finally release the temp files
            File.Delete(tempTdFilename);
            File.Delete(tempTdFilename);
        }
    }
}
