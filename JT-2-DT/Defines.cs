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
	
	public const int HeuristicSolverTimeout = 5 * 60 * 1000;
	public const int ExactSolverTimeout = 5 * 60 * 1000;
	public const int C2dTimeout = 25 * 60 * 1000;
	public const int BaselineTimeout = 30 * 60 * 1000;
	
	public const int InstanceLimit = 8;
}
