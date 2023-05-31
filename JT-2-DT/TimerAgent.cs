using System.Diagnostics;

namespace JT_2_DT;

public class TimerAgent : IDisposable
{
	bool _disposed = false;
	Stopwatch _timer = Stopwatch.StartNew();
	
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	
	protected virtual void Dispose(bool disposing) 
	{
		if (_disposed) 
		{
			return;
		}
		
		Debug.WriteLine($"runtime = {_timer.Elapsed.TotalMilliseconds}ms");
		_disposed = true;
	}
}
