using System.Runtime.InteropServices;

namespace JT_2_DT;

public static class Defines 
{
	public static string OsSuffix { get 
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
		{
			return "windows.exe";
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) 
		{
			return "linux";
		}
		else 
		{
			throw new NotImplementedException("unsupported operating system");
		}
	}}
	
	public const int HeuristicSolverTimeout = 1 * 60 * 1000;
	public const int ExactSolverTimeout = 1 * 60 * 1000;
	public const int DtreeTimeout = 10 * 60 * 1000;
	public const int C2dTimeout = 20 * 60 * 1000;
	public const int BaselineTimeout = 30 * 60 * 1000;
	
	public const int InstanceLimit = 3;
	
	public const int TotalJobLimit = 12;
	
	public static string? ExeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
}
