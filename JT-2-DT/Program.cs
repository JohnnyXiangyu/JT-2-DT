using JT_2_DT;
using System.Diagnostics;

// requesting temp files
using TempFileAgent tdFileAgent = new();
using TempFileAgent grFileAgent = new();
using TempFileAgent dtreeFileAgent = new();

string tempTdFilename = tdFileAgent.TempFilePath;
string tempGrFilename = grFileAgent.TempFilePath;
string tempDtreeFilename = dtreeFileAgent.TempFilePath;

// overall arguments
string cnfPath = string.Empty;
bool useCleanBuild = false;
string mode = "--dnnf";

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
		mode = args[0];
	}
	if (args.Length >= 2)
	{
		cnfPath = args[1];
	}
	if (args.Length == 3)
	{
		useCleanBuild = args[2] == "--clean";
	}
}

using TimerAgent timer = new();

// moralization
Cnf formula = new(cnfPath);
MoralGraph graph = new(formula);

// if mode is moral graph, break here
if (mode == "--moral-graph") 
{
	Console.WriteLine(graph.Serialize());
	return;
}

graph.OutputToFile(tempGrFilename);

// solver
ITwSolver solver = new JT_2_DT.Solvers.Heuristic.MinFillBgMrs();
solver.Execute(tempGrFilename, tempTdFilename);

// dtree compilation
Dtree dtree = new(tempTdFilename, formula.Clauses, useCleanBuild);

if (mode == "--dtree") 
{
	foreach (string line in dtree.SerializeAsDtree()) 
	{
		Console.WriteLine(line);
	}
	return;
}

if (mode != "--dnnf") 
{
	throw new ArgumentException("unrecognized mode");
}

File.WriteAllLines(tempDtreeFilename, dtree.SerializeAsDtree());

// c2d invocation
string c2dPath = Path.Combine("external_executables", $"c2d_{Defines.OsSuffix}");
using (Process c2dInstance = new())
{
	Console.WriteLine(File.Exists(c2dPath));
	
	c2dInstance.StartInfo.UseShellExecute = false;
	c2dInstance.StartInfo.FileName = c2dPath;
	c2dInstance.StartInfo.Arguments = $"-in {cnfPath} -dt_in {tempDtreeFilename}";
	c2dInstance.Start();
	c2dInstance.WaitForExit();
}
