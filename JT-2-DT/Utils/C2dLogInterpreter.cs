using System.Diagnostics;
using System.Text.RegularExpressions;

namespace JT_2_DT.Utils;

public class C2dLogInterpreter 
{
	public double CompileTime = double.MaxValue;
	public double TotalTime = double.MaxValue;
	public string ModelCount = "undefined";
	public double ModelCountTime = double.MaxValue;
	public int Width = 0;
	
	static Regex s_ModelCountPattern = new(@"Counting...(?<count>\d+) models / (?<time>\d+\.\d+)s");
	static Regex s_C2dTotalTimePattern = new(@"Total Time: (?<sec>.+)s");
	static Regex s_C2dCompileTimePattern = new(@"Compile Time: (?<ctime>.+)s / Pre-Processing: (?<pretime>.+)s / Post-Processing: (?<posttime>.+)s");
	static Regex s_C2dWidthPattern = new(@"Max Cluster=(?<cluster>\d+)");
		
	private Stopwatch timer = new();
		
	public bool ProcessLog(string x) 
	{		
		Match match;
		if ((match = s_C2dWidthPattern.Match(x)).Success) 
		{
			timer.Start();
			Width = int.Parse(match.Groups["cluster"].Value) - 1;
		}
		else if (s_C2dCompileTimePattern.IsMatch(x)) 
		{
			timer.Stop();
			CompileTime = timer.Elapsed.TotalSeconds;
		}
		else if ((match = s_C2dTotalTimePattern.Match(x)).Success) 
		{
			TotalTime = double.Parse(match.Groups["sec"].Value);
		}
		else if ((match = s_ModelCountPattern.Match(x)).Success) 
		{
			ModelCount = match.Groups["count"].Value;
			ModelCountTime = double.Parse(match.Groups["time"].Value);
		}
		else 
		{
			return false;
		}
		
		return true;
	}
	
	public Task Reader(Process c2dInstance) => Task.Run(() =>
	{
		string? c2dOutputLine = null;
		while ((c2dOutputLine = c2dInstance.StandardOutput.ReadLine()) != null)
		{
			_ = ProcessLog(c2dOutputLine);
		}
	});
}
