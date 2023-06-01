namespace JT_2_DT.Utils;

public class TempFileAgent : IDisposable
{
	bool _disposed = false;
	public string TempFilePath {get;set;}
	
	public TempFileAgent() 
	{
		TempFilePath = Path.GetTempFileName();
	}
	
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
		
		if (disposing) 
		{
			File.Delete(TempFilePath);
		}
		
		_disposed = true;
	}
}