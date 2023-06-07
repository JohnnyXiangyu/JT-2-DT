using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

namespace JT_2_DT.ExecutionModes;

public class BaselineBenchmark 
{
	Dictionary<string, string> _baselineModelCounts = new();
	Dictionary<string, double> _baselineTotalTime = new();
	Dictionary<string, double> _baselineCompileTime = new();
	Dictionary<string, long> _baselineNnfSize = new();
	Dictionary<string, int> _baselineWidth = new();
	Dictionary<string, bool> _baselineSuccess = new();

	IEnumerable<string> _benchMarkFolders;
	
	public BaselineBenchmark(IEnumerable<string> folders)
	{
		_benchMarkFolders = folders;
	}
	
	public void Run()  
	{
		StringBuilder csvHeader = new();
		csvHeader.Append("CNF, ");
		csvHeader.Append("Dtree width vanilla, ");
		csvHeader.Append("NNF Size Vanilla, ");
		csvHeader.Append("DNNF Compilation Time Vanilla, ");
		csvHeader.Append("Total Time Vanilla, ");
		csvHeader.Append("Model Count vanilla, ");
		Console.WriteLine(csvHeader.ToString());
		
		Utils.BatchGenerator batcher = new(_benchMarkFolders, Defines.InstanceLimit);
	
		foreach (var cnfFiles in batcher.LoadBenchmarks())
		{
			Console.Error.WriteLine("next benchmarks:");
			foreach (string file in cnfFiles)
			{
				Console.Error.Write("    ");
				Console.Error.WriteLine(file);
			}
			Console.Error.WriteLine("");

			// prepare baseline
			foreach (string file in cnfFiles)
			{
				_baselineModelCounts[file] = "0";
				_baselineTotalTime[file] = double.MaxValue;
				_baselineCompileTime[file] = double.MaxValue;
				_baselineNnfSize[file] = int.MaxValue;
				_baselineWidth[file] = 0;
				_baselineSuccess[file] = true;
			}

			// start baseline tasks
			List<Task> baseLineTasks = new();
			foreach (string cnfFile in cnfFiles)
			{
				baseLineTasks.Add(RunBaseline(cnfFile));
			}
			
			Task.WaitAll(baseLineTasks.ToArray());
			Console.Error.WriteLine("batch done \n");
		}
	}
	
	private Task RunBaseline(string cnfFile) => Task.Run(async ()=>
	{
		using Utils.TempFileAgent tempCnf = new();
		string cnfPath = tempCnf.TempFilePath;
		{
			using FileStream originalCnf = File.OpenRead(cnfFile);
			using FileStream tempCnfFile = File.OpenWrite(tempCnf.TempFilePath);
			await originalCnf.CopyToAsync(tempCnfFile);
		}

		using Process c2dInstance = new();
		c2dInstance.StartInfo.FileName = Path.Combine("external_executables", $"c2d_{Defines.OsSuffix}");
		c2dInstance.StartInfo.Arguments = $"-in {cnfPath} -count -smooth_all -reduce";
		c2dInstance.StartInfo.RedirectStandardOutput = true;
		c2dInstance.Start();

		Utils.C2dLogInterpreter interpreter = new();
		Task readerTask = interpreter.Reader(c2dInstance);

		bool success = c2dInstance.WaitForExit(Defines.BaselineTimeout);
		if (!success)
		{
			c2dInstance.Kill();
			_baselineSuccess[cnfFile] = false;
		}
		
		await readerTask;

		_baselineModelCounts[cnfFile] = interpreter.ModelCount;
		_baselineCompileTime[cnfFile] = interpreter.CompileTime;
		_baselineTotalTime[cnfFile] = interpreter.TotalTime;
		_baselineWidth[cnfFile] = interpreter.Width;

		if (File.Exists(cnfPath + ".nnf"))
		{
			FileInfo dnnfInfo;
			dnnfInfo = new(cnfPath + ".nnf");
			_baselineNnfSize[cnfFile] = dnnfInfo.Length;
			File.Delete(cnfPath + ".nnf");
		}

		if (_baselineSuccess[cnfFile])
		{
			Console.Error.WriteLine($"baseline finished: {cnfFile}");
		}
		else
		{
			Console.Error.WriteLine($"benchmark failed: {cnfFile}");
		}
		
		Utils.DataBuilder dataBuilder = new();
		dataBuilder.Append(Path.GetFileName(cnfFile));
		dataBuilder.Append(_baselineSuccess[cnfFile]);
		dataBuilder.Append(_baselineWidth[cnfFile]);
		dataBuilder.Append(_baselineNnfSize[cnfFile]);
		dataBuilder.Append(_baselineCompileTime[cnfFile]);
		dataBuilder.Append(_baselineTotalTime[cnfFile]);
		dataBuilder.Append(_baselineModelCounts[cnfFile]);
		Console.WriteLine(dataBuilder.ToString());
	});
}