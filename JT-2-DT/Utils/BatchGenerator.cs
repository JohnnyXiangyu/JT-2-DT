namespace JT_2_DT.Utils;

public class BatchGenerator 
{
	IEnumerable<string> _benchMarkFolders;
	int _batchSize;
	
	public BatchGenerator(IEnumerable<string> folders, int batchSize) 
	{
		_benchMarkFolders = folders;
		_batchSize = batchSize;
	}
	
	public IEnumerable<List<string>> LoadBenchmarks()
	{
		List<string> cnfFiles = new();
		foreach (string file in NextBenchmark())
		{
			if (cnfFiles.Count >= _batchSize)
			{
				yield return cnfFiles;
				cnfFiles = new();
			}

			cnfFiles.Add(file);
		}

		yield return cnfFiles;
	}

	private IEnumerable<string> NextBenchmark()
	{
		foreach (string folder in _benchMarkFolders)
		{
			DirectoryInfo d = new DirectoryInfo(Path.Combine("Examples", folder));
			FileInfo[] Files = d.GetFiles("*.cnf");

			foreach (FileInfo file in Files)
			{
				yield return file.FullName;
			}
		}
	}
}
